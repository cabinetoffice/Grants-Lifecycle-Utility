using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Newtonsoft.Json;

public class UpdateSchemeStagePlugin : IPlugin
{
    private readonly string _unsecureConfig;

    public UpdateSchemeStagePlugin(string unsecureConfig, string secureConfig)
    {
        _unsecureConfig = unsecureConfig ?? string.Empty;
        // secureConfig is unused in this example
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

        // Parse and validate unsecure configuration
        var stageMappings = ParseConfig(_unsecureConfig);
        if (stageMappings == null || stageMappings.Count == 0)
        {
            throw new InvalidPluginExecutionException("Unsecure configuration is invalid or not provided.");
        }

        // Proceed with existing plugin logic (entity processing, field detection, etc.)
        if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
        {
            var preImage = (Entity)context.PreEntityImages["PreImage"];
            string[] monitoredFields = {
                "gap_awaitinglaunchcompletedon",
                "gap_frontdoorcompletedon",
                "gap_assessmentcompletedon",
                "gap_mobilisationcompletedon",
                "gap_monitoringcompletedon",
                "gap_premobilisationcompletedon"
            };
            var changedField = monitoredFields.FirstOrDefault(field =>
                entity.Attributes.Contains(field) &&
                entity[field] != null &&
                (!preImage.Contains(field) || preImage[field] == null)
            );
            if (string.IsNullOrEmpty(changedField) || !stageMappings.ContainsKey(changedField))
            {
                return;
            }
            var nextStageName = stageMappings[changedField];

            // Retrieve the scheme record associated with this BPF instance
            var schemeQuery = new QueryExpression("gap_scheme")
            {
                ColumnSet = new ColumnSet("gap_stage"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("gap_schemeid", ConditionOperator.Equal, entity.Id)
                    }
                }
            };
            var schemeEntity = service.RetrieveMultiple(schemeQuery).Entities.FirstOrDefault();
            if (schemeEntity == null)
            {
                throw new InvalidPluginExecutionException("No scheme record found for the current BPF instance.");
            }
            var gapStageValue = GetStageValue(nextStageName);
            var schemeUpdate = new Entity("gap_scheme", schemeEntity.Id)
            {
                ["gap_stage"] = new OptionSetValue(gapStageValue)
            };
            service.Update(schemeUpdate);

            // Move the BPF to the next stage
            MoveBpfToNextStage(service, entity.Id, nextStageName);
        }
    }

    private void MoveBpfToNextStage(IOrganizationService service, Guid schemeId, string nextStageName)
    {
        var bpfQuery = new QueryExpression("gap_schemeadministrationprocess")
        {
            ColumnSet = new ColumnSet("activestageid", "processid"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("bpf_gap_schemeid", ConditionOperator.Equal, schemeId),
                    new ConditionExpression("statecode", ConditionOperator.Equal, 0) // Active
                }
            }
        };
        var bpfInstance = service.RetrieveMultiple(bpfQuery).Entities.FirstOrDefault();
        if (bpfInstance == null)
        {
            throw new InvalidPluginExecutionException("No active BPF instance found for the current record.");
        }
        var processId = bpfInstance.GetAttributeValue<EntityReference>("processid")?.Id;
        if (processId == null)
        {
            throw new InvalidPluginExecutionException("No process id found for the active BPF.");
        }
        var stageQuery = new QueryExpression("processstage")
        {
            ColumnSet = new ColumnSet("processstageid"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("stagename", ConditionOperator.Equal, nextStageName),
                    new ConditionExpression("processid", ConditionOperator.Equal, processId)
                }
            }
        };
        var stageResult = service.RetrieveMultiple(stageQuery).Entities.FirstOrDefault();
        if (stageResult == null)
        {
            throw new InvalidPluginExecutionException($"No matching stage found with name: {nextStageName} in the current BPF.");
        }
        Guid nextStageId = stageResult.Id;
        var bpfUpdate = new Entity("gap_schemeadministrationprocess", bpfInstance.Id)
        {
            ["activestageid"] = new EntityReference("processstage", nextStageId)
        };
        service.Update(bpfUpdate);
    }

    private Dictionary<string, string> ParseConfig(string configuration)
    {
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(configuration);
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException("Error parsing configuration: " + ex.Message);
        }
    }

    private int GetStageValue(string stageName)
    {
        switch (stageName)
        {
            case "Front Door": return 803260006;
            case "Pre-Mobilisation": return 803260000;
            case "Mobilisation": return 803260001;
            case "Awaiting Launch": return 803260002;
            case "Monitoring": return 803260008;
            case "Assessment": return 803260003;
            default: throw new InvalidPluginExecutionException("Stage name not recognized.");
        }
    }
}