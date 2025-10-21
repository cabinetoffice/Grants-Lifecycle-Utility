using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;



public class GenerateUserList : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        try
        {
            // Capturing the Team ID through one of the request/input parameters.
            // The ID must be a GUID value.
            Guid teamId = (Guid)context.InputParameters["TeamId"];

            // Retriveing links between Teams and Users through the teammembership table.
            QueryExpression teamMembersQuery = new QueryExpression("teammembership")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                {
                    new ConditionExpression("teamid", ConditionOperator.Equal, teamId),
                }
                }
            };
            EntityCollection foundTeamMembers = service.RetrieveMultiple(teamMembersQuery);

            // Looping through the records to extract the User IDs from each row
            // and place into a List.
            List<dynamic> members = new List<dynamic>();
            foreach (Entity teamMember in foundTeamMembers.Entities)
            {
                members.Add(teamMember["systemuserid"]);
            }

            // Turning the List into a JSON String to then be parsed back to JSON
            // in the custom page.
            string json = JsonSerializer.Serialize(members);
            context.OutputParameters.Add("UserList", json);
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException($"{ex}");
        }
    }

}