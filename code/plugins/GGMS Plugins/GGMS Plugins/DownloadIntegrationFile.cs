using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;



public class DownloadIntegrationFile : IPlugin
{
    static List<int> generateFlagArray(List<string> foundFlags)
    {
        List<int> generated = new List<int>();
        // For the time being, since conditional formatting is not present on the .xlsx file, if the field
        // contains one of the flags we set it as it says. For when it doesn't (e.g. numerical) we set default AMBER
        foreach (string flag in foundFlags)
        {
            if (flag.ToLower().Contains("green"))
            {
                generated.Add(803260000);
            }
            else if (flag.ToLower().Contains("amber"))
            {
                generated.Add(803260001);
            }
            else if (flag.ToLower().Contains("red"))
            {
                generated.Add(803260002);
            }
            else
            {
                generated.Add(803260003);
            }
        }
        return generated;
    }

    public static Entity FindExistingActiveAlert(IOrganizationService service, String applicationId, String flagName)
    {
        QueryExpression existingAlertQuery = new QueryExpression("gap_alert")
        {
            ColumnSet = new ColumnSet("gap_alertid"),
            Criteria = new FilterExpression(LogicalOperator.And)
            {
                Conditions =
                {
                    new ConditionExpression("gap_application", ConditionOperator.Equal, Guid.Parse(applicationId) ),
                    new ConditionExpression("gap_title", ConditionOperator.Equal, flagName),
                    new ConditionExpression("gap_archived", ConditionOperator.Equal, false)
                }
            }
        };
        EntityCollection alertResults = service.RetrieveMultiple(existingAlertQuery);
        if (alertResults.Entities.Count > 0)
        {
            Guid foundAlertId = alertResults.Entities[0].Id;
            Entity entity = service.Retrieve("gap_alert", foundAlertId, new ColumnSet(true));
            return entity;
        }
        else
        {
            return null;
        }
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        try
        {
            // Getting integration file record
            EntityReference reference = (EntityReference)context.InputParameters["Target"];
            Entity entity = service.Retrieve(reference.LogicalName, reference.Id, new ColumnSet("gap_integrationfileid"));

            // Starting integration file download. Getting download blocks and then putting them together to form a file
            var initializeFileBlocksDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = reference,
                FileAttributeName = "gap_fileupload"
            };
            var initializeFileBlocksDownloadResponse = (InitializeFileBlocksDownloadResponse)service.Execute(initializeFileBlocksDownloadRequest);

            DownloadBlockRequest downloadBlockRequest = new DownloadBlockRequest
            {
                FileContinuationToken = initializeFileBlocksDownloadResponse.FileContinuationToken
            };
            var downloadBlockResponse = (DownloadBlockResponse)service.Execute(downloadBlockRequest);

            // Array to list the columns that contain the flags we want to capture, hard coded for now
            //string[] flagsToFind = { "Company age in years", "Company status", "Company accounts", "Last insolvency", "Number of directors", "Virtual mailbox risk", "Multiple Association Risk" };
            string[] companyExclusionColumnsPound = { "Spotlight reference number", "Grant awarded", "Date created", "Application reference", "Organisation name", "Charity number", "Company number", "Amount", "Address street", "Address town", "Address county", "Address postcode", "Companies House registered address", "Company type", "Nature of business (SIC)", "Incorporation date", "Company account last submitted", "Company account next due", "Charity status", "Data Source", "What the charity does", "Charity age in years", "Creation date", "Charity income (£)", "Latest financial year start date", "Latest financial year end date", "Accounts status", "Income vs Grant amount", "Charity primary postcode match", "Interim manager appointed", "Date of appointment", "Last updated", "Trustee 1", "Has any awarded Grants?", "Funders", "Awarded Grants value", "Number of awarded Grants", "Has awarded Grant", "Has contracts", "Funder", "Number of contracts", "Contracts value", "Has applications in Spotlight", "Number of applications recorded in Spotlight", "Total value of other applications recorded in Spotlight", "Number of applications stored in SP from other organisations with an identical postcode", "Number of other applications in Spotlight with matching directors", "Value of other applications in Spotlight with matching directors", "Names of directors with other appointments linked to applications in Spotlight", "Director 1", "Director 2", "Director 3", "Director 4", "Director 5", "Director 6", "Director 7", "Director 8", "Director 9", "Director 10", "Director 11", "Director 12", "Disqualified Director 1", "PSC 1 - Kind", "PSC 1 - Name", "PSC 1 - Nationality", "PSC 1 - Natures of Control", "PSC 1 - Sanctions", "PSC 2 - Kind", "PSC 2 - Name", "PSC 2 - Nationality", "PSC 2 - Natures of Control", "PSC 2 - Sanctions", "PSC 3 - Kind", "PSC 3 - Name", "PSC 3 - Nationality", "PSC 3 - Natures of Control", "PSC 3 - Sanctions" };
            string[] charityExclusionColumnsPound = { "Spotlight reference number", "Grant awarded", "Date created", "Application reference", "Organisation name", "Charity number", "Company number", "Amount", "Address street", "Address town", "Address county", "Address postcode", "Companies House registered address", "Company type", "Nature of business (SIC)", "Incorporation date", "Company account last submitted", "Company account next due", "Data Source", "What the charity does", "Creation date", "Charity income (£)", "Latest financial year start date", "Latest financial year end date", "Accounts status", "Date of appointment", "Last updated", "Trustee 1", "Has any awarded Grants?", "Funders", "Awarded Grants value", "Number of awarded Grants", "Has awarded Grant", "Has contracts", "Funder", "Number of contracts", "Contracts value", "Has applications in Spotlight", "Number of applications recorded in Spotlight", "Total value of other applications recorded in Spotlight", "Number of applications stored in SP from other organisations with an identical postcode", "Number of other applications in Spotlight with matching directors", "Value of other applications in Spotlight with matching directors", "Names of directors with other appointments linked to applications in Spotlight", "Director 1", "Director 2", "Director 3", "Director 4", "Director 5", "Director 6", "Director 7", "Director 8", "Director 9", "Director 10", "Director 11", "Director 12", "Disqualified Director 1", "PSC 1 - Kind", "PSC 1 - Name", "PSC 1 - Nationality", "PSC 1 - Natures of Control", "PSC 1 - Sanctions", "PSC 2 - Kind", "PSC 2 - Name", "PSC 2 - Nationality", "PSC 2 - Natures of Control", "PSC 2 - Sanctions", "PSC 3 - Kind", "PSC 3 - Name", "PSC 3 - Nationality", "PSC 3 - Natures of Control", "PSC 3 - Sanctions" };
            string[] charityExclusionColumns = charityExclusionColumnsPound.Select(s => s.Replace("\u00A3", "GBP")).ToArray();
            string[] companyExclusionColumns = companyExclusionColumnsPound.Select(s => s.Replace("\u00A3", "GBP")).ToArray();
            // Reading CSV using the Visual Basic library
            using (var reader = new StringReader(Encoding.UTF8.GetString(downloadBlockResponse.Data)))
            using (var parser = new TextFieldParser(reader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                service.Update(entity);
                // .ReadFields() function acts as a pointer to a line in the CSV, whenever it is called it reads the contents of the line it is on as an array
                // and then moves the pointer over to the next line until it is called again.
                // Loop to move past the first 5 rows of data until the the actual spotlight table is reached.
                Enumerable.Range(0, 5).ToList().ForEach(arg => parser.ReadFields());
                if (!parser.EndOfData)
                {
                    string[] headersPound = parser.ReadFields();
                    string[] headers = headersPound.Select(s => s.Replace("\u00A3", "GBP")).ToArray();
                    while (!parser.EndOfData)
                    {
                        string[] currentApplication = parser.ReadFields();
                        service.Update(entity);
                        List<string> foundFlags = new List<string>();
                        List<string> flagData = new List<string>();
                        var applicationIdExtract = currentApplication[3];
                        // Spotlight CSV contains the GAP ID of the application, this ID is not the primary key of the applications table
                        // A query needs to be made to search rows and query by the GAP ID colu mn.
                        QueryExpression query = new QueryExpression("gap_application")
                        {
                            ColumnSet = new ColumnSet("gap_applicationid"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("gap_gapid", ConditionOperator.Equal, applicationIdExtract)
                                }
                            }
                        };
                        EntityCollection results = service.RetrieveMultiple(query);
                        Entity applicaitonEntity = null;
                        Guid applicationId = Guid.Empty;
                        if (results.Entities.Count > 0)
                        {
                            applicaitonEntity = results.Entities[0];
                            applicationId = applicaitonEntity.Id;
                            EntityReference applicationEntityReference = new EntityReference("gap_application", applicationId);
                            var charityNumberIndex = Array.IndexOf(headers, "Charity number");
                            string[] flagsToFindIncome;
                            var isCharity = currentApplication[charityNumberIndex];
                            if (isCharity != "" && isCharity != "NA")
                            {
                                flagsToFindIncome = headers.Except(charityExclusionColumns).ToArray();
                            }
                            else
                            {
                                flagsToFindIncome = headers.Except(companyExclusionColumns).ToArray();
                            }
                            string[] flagsToFind = flagsToFindIncome.Where(s => !s.Contains("Charity income")).ToArray();
                            foreach (string flag in flagsToFind)
                            {
                                var flagIndex = Array.IndexOf(headers, flag);
                                foundFlags.Add(currentApplication[flagIndex]);
                            }
                            var generatedFlags = generateFlagArray(foundFlags);

                            for (var i = 0; i < foundFlags.Count; i++)
                            {
                                // Find existing alerts
                                Entity activeAlert = FindExistingActiveAlert(service, applicationId.ToString(), flagsToFind[i]);
                                if (activeAlert != null)
                                {
                                    EntityReference alertEntityReference = new EntityReference("gap_alert", activeAlert.Id);
                                    OptionSetValue typeOptionSet = (OptionSetValue)activeAlert["gap_type"];
                                    if (typeOptionSet.Value != generatedFlags[i])
                                    {
                                        Entity updatedAlertEntity = new Entity("gap_alert");
                                        activeAlert["gap_archived"] = true;
                                        updatedAlertEntity["gap_archived"] = false;
                                        updatedAlertEntity["gap_title"] = flagsToFind[i];
                                        updatedAlertEntity["gap_alertdata"] = foundFlags[i];
                                        if (generatedFlags[i] == 803260000)
                                        {
                                            updatedAlertEntity["gap_alertstatus"] = new OptionSetValue(803260002);
                                        }
                                        else
                                        {
                                            updatedAlertEntity["gap_alertstatus"] = new OptionSetValue(803260003);
                                        }
                                        updatedAlertEntity["gap_application"] = applicationEntityReference;
                                        updatedAlertEntity["gap_type"] = new OptionSetValue(generatedFlags[i]);
                                        updatedAlertEntity["gap_linkedalert"] = alertEntityReference;
                                        activeAlert["gap_linkedalert"] = alertEntityReference;
                                        service.Create(updatedAlertEntity);
                                        service.Update(activeAlert);
                                    }
                                }
                                else
                                {
                                    Entity newAlertEntity = new Entity("gap_alert");
                                    newAlertEntity["gap_title"] = flagsToFind[i];
                                    newAlertEntity["gap_alertdata"] = foundFlags[i];
                                    if (generatedFlags[i] == 803260000)
                                    {
                                        newAlertEntity["gap_alertstatus"] = new OptionSetValue(803260002);
                                    }
                                    else
                                    {
                                        newAlertEntity["gap_alertstatus"] = new OptionSetValue(803260003);
                                    }
                                    newAlertEntity["gap_application"] = applicationEntityReference;
                                    newAlertEntity["gap_type"] = new OptionSetValue(generatedFlags[i]);
                                    newAlertEntity["gap_archived"] = false;
                                    service.Create(newAlertEntity);
                                }
                            }
                        }
                        else
                        {
                        }
                        entity["gap_processed"] = true;
                        service.Update(entity);
                    }
                }

            }
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException($"{ex}");
        }
    }

}