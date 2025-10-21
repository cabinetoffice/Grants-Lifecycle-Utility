using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;



public class ProcessApplicantResponses : IPlugin
{
    // Function to sort the list of sections, questions, and answers.
    // The function takes two input parameters, the anwers and the preferred order of questions
    public List<dynamic> SortQuestions(List<dynamic> responses, List<dynamic> questionOrder)
    {
        //List<string> questionOrder = new List<string> { "Lead Applicant", "GAP ID", "Applying for", "Submitted on", "Organisation Name", "Organisation type", "Address line 1", "Address line 2", "Address city", "Address county", "Address postcode", "Charities Commission number if the organisation has one (if blank, number has not been entered)", "Companies House number if the organisation has one (if blank, number has not been entered)", "Amount applied for", "Where funding will be spent", "Provide your Project Title", "Please confirm if you are appying as Track 1 or Track 2.", "Please provide a short description of your proposed project.", "Please provide an indication of the topic of your proposal.", "Lead Organisation Name", "Lead Organisation Contact Name", "Lead Organisation Contact Email", "Lead Organisation Contact Phone Number", "Lead Organisation Contact Registered Address", "Lead Organisation Postcode", "Please populate with the relevant company or charity registration number", "Please provide a short overview of your company’s background (e.g. core activities)", "Please select your organisation size from the list", "Please select your Subsidy Control Status from the list", "Partner Organisation OnePartner Organisation Name", "Partner Organisation OnePartner Organisation Contact Name", "Partner Organisation OnePartner Organisation Contact Email", "Partner Organisation OnePartner Organisation Registered Address", "Partner Organisation OnePartner Organisation Post Code", "Partner Organisation OnePlease populate with the relevant company or charity registration number for the partner organisation.", "Partner Organisation OnePlease select your organisation size from the list", "Partner Organisation OnePlease select your Subsidy Control Status from the list", "Partner Organisation TwoPartner Organisation Name", "Partner Organisation TwoPartner Organisation Contact Name", "Partner Organisation TwoPartner Organisation Contact Email", "Partner Organisation TwoPartner Organisation Registered Address", "Partner Organisation TwoPartner Organisation Post Code", "Partner Organisation TwoPlease populate with the relevant company or charity registration number for the partner organisation.", "Partner Organisation TwoPlease select your organisation size from the list", "Partner Organisation TwoPlease select your Subsidy Control Status from the list", "Partner Organisation ThreePartner Organisation Name", "Partner Organisation ThreePartner Organisation Contact Name", "Partner Organisation ThreePartner Organisation Contact Email", "Partner Organisation ThreePartner Organisation Registered Address", "Partner Organisation ThreePartner Organisation Post Code", "Partner Organisation ThreePlease populate with the relevant company or charity registration number for the partner organisation.", "Partner Organisation ThreePlease select your organisation size from the list", "Partner Organisation ThreePlease select your Subsidy Control Status from the list", "Partner Organisation FourPartner Organisation Name", "Partner Organisation FourPartner Organisation Contact Name", "Partner Organisation FourPartner Organisation Contact Email", "Partner Organisation FourPartner Organisation Registered Address", "Partner Organisation FourPartner Organisation Post Code", "Partner Organisation FourPlease populate with the relevant company or charity registration number for the partner organisation.", "Partner Organisation FourPlease select your organisation size from the list", "Partner Organisation FourPlease select your Subsidy Control Status from the list", "Please submit summary of your high level financial details using the \"High Level Financial details\" template.", "FOR ENTERPRISES ONLY: Have the Lead Organisation or any Partner Organisations received any other grants from UK Space Agency or any other government body in the last 3 financial years?", "Has this project/activity received any other grants from UK Space Agency or any other government body in the last 3 years?", "Have you applied for any other grants or funding awards in relation to the work in this proposal?", "What proportion of the overall turnover for your organisation is the grant value for this proposal?", "Project Summary", "How does your proposal advance the strategic goals identified within the call document and the wider goals of the UK Space Agency?", "The spending of grant funding should deliver Value for Money to the UK taxpayer, including robust financial accounting practices. Why do you need grant funding, how will you spend it and how does this represent good value for money for the taxpayer?", "Please attach a zip file for the supporting documents of the previous question.", "The Agency seeks to ensure that the investment it makes delivers a strong return and supports the space sector’s future growth. How will this project catalyse future investment into the UK space sector?", "Benefits will be tracked throughout the life of funded projects to help understand the impact that investment makes on the sector and any wider benefits to society that emerge. What will be the impact of receiving the grant, for your business and outside?", "How is your idea technically feasible and/or innovative?", "Projects have a greater chance of being delivered to time and budget if they are well managed throughout their delivery. How will you ensure effective delivery of this project throughout its full duration?", "Please attach a zip file for the supporting documents of the previous question.", "Please upload the details of your proposed project milestones through the milestones template. These form part of your agreement with the Agency by detailing the milestones, deliverables and acceptance criteria we agree to in order to release payment.", "Please upload your Budget Breakdown using the template provided in the adverts supporting information.", "Please upload your National Security Questionnaire using the template provided in the adverts supporting information.", "If required, please upload your Additional Overheads Template using the template provided in the adverts supporting information.", "Please upload any relevant CV files.", "If available, please upload any letters of support", "If applicable, please upload a copy of Annex A: Details of Previous Grants for Subsidy Control Purposes", "If applicable, please upload a copy of the Partner Assurance Statement.", "The lead applicant, named on this application, confirms they have obtained the relevant consent from an authorised officer or appropriate signatory who will sign a grant agreement if successful.", "I hereby confirm that I consent to provide the UK Space Agency, if requested, with any and all relevant documents to allow them to perform a due diligence check prior to grant funding being awarded.", "I confirm I have read and accepted the terms of the draft GFA, or requested amendments for any terms and conditions that contradict with existing legal or regulatory obligations. No material changes will be made to this upon award if successful.", "I confirm I have followed eligible expenditure criteria and the submission has been made using only eligible criteria.", "I confirm that all parties (the Lead Organisation and any partners) applying for grant funding have assessed themselves, and meet the requirements, according to the Subsidy Control Regulations contained within the call guidance.", "You confirm that the information provided in this application is complete to the best of your knowledge and that you are actively engaged in this project and responsible for its overall management and agree to administer the award if successful." };
        int defaultIndex = int.MaxValue;

        var orderDict = questionOrder
            .Select((item, index) => new { item.Section, item.Question, index })
            .ToDictionary(x => $"{x.Section}:{x.Question}", x => x.index);

        List<dynamic> sortedObjects = responses
            .OrderBy(item =>
            {
                string key = $"{item.Section}:{item.Question}";
                return orderDict.TryGetValue(key, out int index) ? index : defaultIndex;
            })
            .ToList();

        return sortedObjects;
    }

    // Function to pre-process the answers into a suitable format to include in the GFA
    // Some answers such as address come in a JSON string format. So doing some preprocessing to have it show in a suitable format.
    // Another answer to pre-process, is file attachments. Some answers are full URLs, so processing them into a suitable format that includes the file name and type.
    public string processAnswerString(string arrayString)
    {
        var result = string.Empty;
        if (arrayString.Contains("[") && arrayString.Contains("]"))
        {
            string cleanedString = arrayString.Trim(new char[] { '[', ']' }).Trim();

            // Split by ", " and remove surrounding quotes from each element
            List<string> stringsList = cleanedString
                .Split(new string[] { "\", \"" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Replace("\"", "")) // Remove the leading and trailing quotes
                .ToList();
            result = string.Join(",", stringsList);
        }
        else if (arrayString.Contains("{") && arrayString.Contains("}"))
        {
            JsonDocument jsonObject = JsonDocument.Parse(arrayString);

            JsonElement root = jsonObject.RootElement;
            List<string> values = new List<string>();
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (property.Value.ToString() != "")
                {
                    values.Add(property.Value.ToString());
                }
            }

            result = string.Join(", ", values);
        }
        else if (arrayString.StartsWith("https://"))
        {
            var processUrl = arrayString.Replace("https://", "");
            var urlList = processUrl.Split('/');
            var documentIndexEnd = urlList[4].IndexOf("?");
            var documentName = Uri.UnescapeDataString(urlList[4].Substring(0, documentIndexEnd));
            var documentSplit = documentName.Split('.');
            result = "File Name: " + documentSplit[0] + "\nFile Type: ." + documentSplit[1];
        }
        else
        {
            result = arrayString;
        }
        return result;
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
        try
        {
            EntityReference reference = (EntityReference)context.InputParameters["Target"];
            Entity entity = service.Retrieve(reference.LogicalName, reference.Id, new ColumnSet("gap_applicationid", "gap_scheme"));
            QueryExpression applicantReponsesQuery = new QueryExpression("gap_response")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                {
                    new ConditionExpression("gap_application", ConditionOperator.Equal, entity.Id )
                    //new ConditionExpression("gap_answer", ConditionOperator.NotNull),
                }
                }
            };
            QueryExpression applicantSections = new QueryExpression("gap_section")
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                {
                    new ConditionExpression("gap_application", ConditionOperator.Equal, entity.Id ),
                }
                }
            };
            // Get Scheme
            EntityReference schemeReference = (EntityReference)entity["gap_scheme"];
            Entity scheme = service.Retrieve("gap_scheme", schemeReference.Id, new ColumnSet("gap_schemeid", "gap_gfatemplateconfiguration"));
            // Get GFA Config
            EntityReference gfaTemplateConfigReference = (EntityReference)scheme["gap_gfatemplateconfiguration"];
            Entity gfaTemplateConfig = service.Retrieve("gap_gfaschemetemplate", gfaTemplateConfigReference.Id, new ColumnSet(true));

            // Starting integration file download. Getting download blocks and then putting them together to form a file
            var initializeFileBlocksDownloadRequest = new InitializeFileBlocksDownloadRequest
            {
                Target = gfaTemplateConfigReference,
                FileAttributeName = "gap_applicationquestions"
            };
            var initializeFileBlocksDownloadResponse = (InitializeFileBlocksDownloadResponse)service.Execute(initializeFileBlocksDownloadRequest);

            DownloadBlockRequest downloadBlockRequest = new DownloadBlockRequest
            {
                FileContinuationToken = initializeFileBlocksDownloadResponse.FileContinuationToken
            };
            var downloadBlockResponse = (DownloadBlockResponse)service.Execute(downloadBlockRequest);
            var questionOrder = new List<dynamic>();

            // Reading CSV using the Visual Basic library
            using (var reader = new StringReader(Encoding.UTF8.GetString(downloadBlockResponse.Data)))
            using (var parser = new TextFieldParser(reader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                if (!parser.EndOfData)
                {
                    while (!parser.EndOfData)
                    {
                        string[] currentSectionQuestion = parser.ReadFields();
                        if (currentSectionQuestion[2] != "duediligence")
                        {
                            dynamic questionOrderObject = new ExpandoObject();
                            questionOrderObject.Section = currentSectionQuestion[0];
                            questionOrderObject.Question = currentSectionQuestion[1];
                            questionOrder.Add(questionOrderObject);
                        }
                    }
                }
            }

            EntityCollection foundResponses = service.RetrieveMultiple(applicantReponsesQuery);
            List<string> duediligence = new List<string>() { "Organisation details", "Funding", "Eligibility", "Contact Information", "Your organisation" };
            var formattedResponses = new List<dynamic>();
            foreach (Entity applicantResponse in foundResponses.Entities)
            {
                if (applicantResponse.Contains("gap_section") && applicantResponse["gap_section"] is EntityReference sectionReference)
                {
                    // Retrieving Section Data
                    Guid sectionId = sectionReference.Id;
                    string sectionEntityName = sectionReference.LogicalName;
                    Entity foundSection = service.Retrieve(sectionEntityName, sectionId, new ColumnSet(true));
                    if (duediligence.Contains(foundSection["gap_title"]))
                    {

                    }
                    else
                    {
                        // Object to store Question and Answer
                        dynamic responseObject = new ExpandoObject();
                        responseObject.Section = foundSection["gap_title"].ToString().Trim();
                        if (foundSection["gap_title"].ToString().Trim().Contains("Partner Organisation"))
                        {
                            responseObject.Question = responseObject.Section + applicantResponse["gap_question"].ToString().Trim();
                        }
                        else
                        {
                            responseObject.Question = applicantResponse["gap_question"].ToString().Trim();
                        }
                        var answerRef = applicantResponse.GetAttributeValue<string>("gap_answer"); ;
                        if (answerRef == null)
                        {
                            responseObject.Answer = "Answer not provided";
                        }
                        else
                        {
                            responseObject.Answer = processAnswerString(applicantResponse["gap_answer"].ToString());
                        }
                        responseObject.Key = foundSection["gap_key"];
                        formattedResponses.Add(responseObject);
                    }
                }
            }
            List<dynamic> sortedFormattedResponses = formattedResponses.OrderBy(responseEl => responseEl.Key).ToList();
            List<dynamic> sortedQuestions = SortQuestions(sortedFormattedResponses, questionOrder);
            var listIndex = 0;
            foreach (dynamic questionClean in sortedQuestions)
            {
                if (questionClean.Section.Contains("Partner Organisation"))
                {
                    //var lengthRemove = questionClean.Section.Length;
                    //sortedQuestions[listIndex].Question.Remove(lengthRemove);
                }
                listIndex++;
            }
            string json = JsonSerializer.Serialize(sortedQuestions);

            context.OutputParameters.Add("ProcessResult", json);
        }
        catch (Exception ex)
        {
            throw new InvalidPluginExecutionException($"{ex}");
        }
    }

}