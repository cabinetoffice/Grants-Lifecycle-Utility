using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using UglyToad.PdfPig;


public class ProcessGfaPdf : IPlugin
{

    string ExtractText(byte[] pdfBytes)
    {
        using (var ms = new MemoryStream(pdfBytes, writable: false))
        {
            using (var doc = PdfDocument.Open(ms))
            {
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                return sb.ToString();
            }
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
            Entity entity = service.Retrieve(reference.LogicalName, reference.Id, new ColumnSet("gap_applicationid"));

            QueryExpression gfaQuery = new QueryExpression("gap_document")
            {
                ColumnSet = new ColumnSet("gap_documentid"),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                {
                    new ConditionExpression("cr982_application", ConditionOperator.Equal, reference.Id ),
                    new ConditionExpression("gap_title", ConditionOperator.Equal, "Final GFA Document" ),
                }
                }
            };
            EntityCollection foundGfa = service.RetrieveMultiple(gfaQuery);
            Guid foundDocumentId = foundGfa.Entities[0].Id;
            EntityReference documentReference = new EntityReference("gap_document", foundDocumentId);

            // Starting integration file download. Getting download blocks and then putting them together to form a file
            var initializeFileBlocksDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = documentReference,
                FileAttributeName = "gap_documentupload"
            };
            var initializeFileBlocksDownloadResponse = (InitializeFileBlocksDownloadResponse)service.Execute(initializeFileBlocksDownloadRequest);

            DownloadBlockRequest downloadBlockRequest = new DownloadBlockRequest
            {
                FileContinuationToken = initializeFileBlocksDownloadResponse.FileContinuationToken
            };
            var downloadBlockResponse = (DownloadBlockResponse)service.Execute(downloadBlockRequest);

            var extractedPdf = ExtractText(downloadBlockResponse.Data);

            // Regex Extraction
            Entity newBankingDetails = new Entity("gap_bankingdetails");

            // Name of Main Grant Holder
            string nameOfMainGrantHolderExtract = Regex.Match(extractedPdf, @"(?<=Main Grant Holder\s).*?(?=\sAddress of Grant Holder)").Value;
            newBankingDetails["gap_nameofmaingrantholder"] = nameOfMainGrantHolderExtract;

            // Address of Grant Holder
            string addressOfGrantHolderExtract = Regex.Match(extractedPdf, @"(?<=Address of Grant Holder\s).*?(?=\sContact telephone number)").Value;
            newBankingDetails["gap_addressofgrantholder"] = addressOfGrantHolderExtract;

            // Contact telephone number
            string contactTelephoneNumberExtract = Regex.Match(extractedPdf, @"(?<=Contact telephone number\s).*?(?=\sGrant name)").Value;
            newBankingDetails["gap_contacttelephonenumber"] = contactTelephoneNumberExtract;

            // Grant name
            string grantNameExtract = Regex.Match(extractedPdf, @"(?<=Grant name\s).*?(?=\sGrant determination number)").Value;
            newBankingDetails["gap_grantname"] = grantNameExtract;

            // Grant determination number
            string grantDeterminationNumberExtract = Regex.Match(extractedPdf, @"(?<=Grant determination number\s).*?(?=\sPart 2)").Value;
            newBankingDetails["gap_grantdeterminationnumber"] = grantDeterminationNumberExtract;

            // Bank / Building Society Name
            string bankBuildingSocietyNameExtract = Regex.Match(extractedPdf, @"(?<=Bank Details Bank / Building Society name\s).*?(?=\sBranch name)").Value;
            newBankingDetails["gap_bankorbuildingsocietyname"] = bankBuildingSocietyNameExtract;

            // Branch name
            string branchNameExtract = Regex.Match(extractedPdf, @"(?<=Branch name\s).*?(?=\sBranch address)").Value;
            newBankingDetails["gap_branchname"] = branchNameExtract;

            // Branch address
            string branchAddressExtract = Regex.Match(extractedPdf, @"(?<=Branch address\s).*?(?=\sAccount name)").Value;
            newBankingDetails["gap_branchaddress"] = branchAddressExtract;

            // Account name
            string accountNameExtract = Regex.Match(extractedPdf, @"(?<=Account name\s).*?(?=\sAccount number)").Value;
            newBankingDetails["gap_accountname"] = accountNameExtract;

            // Account number
            string accountNumberExtract = Regex.Match(extractedPdf, @"(?<=Account number\s).*?(?=\sBank sort code)").Value;
            newBankingDetails["gap_accountnumber"] = accountNumberExtract;

            // Bank sort code
            string bankSortCodeExtract = Regex.Match(extractedPdf, @"(?<=Bank sort code\s).*?(?=\sBuilding society roll number)").Value;
            newBankingDetails["gap_banksortcode"] = bankSortCodeExtract;

            // Building society roll number
            string buildingSocietyRollNumberExtract = Regex.Match(extractedPdf, @"(?<=Building society roll number\s).*?(?=\sAccount type)").Value;
            newBankingDetails["gap_buildingsocietyrollnumber"] = buildingSocietyRollNumberExtract;

            // Account type
            string accountTypeExtract = Regex.Match(extractedPdf, @"(?<=Account type\s).*?(?=\sPart 3)").Value;
            newBankingDetails["gap_accounttype"] = accountTypeExtract;

            newBankingDetails["gap_title"] = "Bank Details for " + nameOfMainGrantHolderExtract;
            Guid newBankingDetailsRecordId = service.Create(newBankingDetails);
            EntityReference newBankingDetailsRef = new EntityReference("gap_bankingdetails", newBankingDetailsRecordId);

            entity["gap_bankingdetails"] = newBankingDetailsRef;
            service.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException($"{ex}");
        }
    }

}