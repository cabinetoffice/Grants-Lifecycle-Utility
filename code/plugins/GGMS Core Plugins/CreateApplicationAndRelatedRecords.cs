using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CreateApplicationAndRelatedRecords : IPlugin {

    public class _Application {
        public string ApplicationId { get; set; }
        public string SubmissionId { get; set; }
        public string ApplicationFormName { get; set; }
        public string EmailAddress { get; set; }
        public DateTime SubmittedOn { get; set; }
        public List<_Section> Sections { get; set; }
    }

    public class _Section {
        public string SectionId { get; set; }
        public string SectionTitle { get; set; }
        public List<_Question> Questions { get; set; }
    }

    public class _Question {
        public string QuestionId { get; set; }
        public string Question { get; set; }
        public object QuestionResponse { get; set; }  // Can be string, list or object
    }

    public void Execute(IServiceProvider serviceProvider) {
        ITracingService tracingService = (ITracingService) serviceProvider.GetService(typeof(ITracingService));
        IPluginExecutionContext context = (IPluginExecutionContext) serviceProvider.GetService(typeof(IPluginExecutionContext));

        if (context.InputParameters.Contains("Target") == false || context.InputParameters["Target"] is EntityReference == false) {
            throw new InvalidPluginExecutionException("An error occurred in CreateApplicationAndRelatedRecords: No target was provided.");
        }
        else if (context.InputParameters.Contains("Body") == false || context.InputParameters["Body"] is string == false) {
            throw new InvalidPluginExecutionException("An error occurred in CreateApplicationAndRelatedRecords: No body was provided.");
        }

        try {
            var factory = (IOrganizationServiceFactory) serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            _Application body = JsonConvert.DeserializeObject<_Application>(context.InputParameters["Body"].ToString());

            if (string.IsNullOrEmpty(body.ApplicationId)) { throw new Exception("An invalid body was provided"); }
            if (string.IsNullOrEmpty(body.SubmissionId)) { throw new Exception("An invalid body was provided"); }
            if (string.IsNullOrEmpty(body.ApplicationFormName)) { throw new Exception("An invalid body was provided"); }
            if (string.IsNullOrEmpty(body.EmailAddress)) { throw new Exception("An invalid body was provided"); }
            if (body.SubmittedOn == default(DateTime)) { throw new Exception("An invalid body was provided"); }
            if (body.Sections == null) { throw new Exception("An invalid body was provided"); }
            foreach (_Section s in body.Sections) {
                if (string.IsNullOrEmpty(s.SectionId)) { throw new Exception("An invalid body was provided"); }
                if (string.IsNullOrEmpty(s.SectionTitle)) { throw new Exception("An invalid body was provided"); }
                if (s.Questions == null) { throw new Exception("An invalid body was provided"); }
                foreach (_Question q in s.Questions) {
                    if (string.IsNullOrEmpty(q.QuestionId)) { throw new Exception("An invalid body was provided"); }
                    if (string.IsNullOrEmpty(q.Question)) { throw new Exception("An invalid body was provided"); }
                    if (q.QuestionResponse == null) { throw new Exception("An invalid body was provided"); }
                }
            }

            EntityReference schemeRef = (EntityReference) context.InputParameters["Target"];
            Entity scheme = service.Retrieve(schemeRef.LogicalName, schemeRef.Id, new ColumnSet(true));

            string schemeName = scheme.GetAttributeValue<string>("gap_title");

            QueryExpression query = new QueryExpression("gap_application") {
                ColumnSet = new ColumnSet("gap_applicationid"),
                Criteria = new FilterExpression {
                    FilterOperator = LogicalOperator.And,
                    Conditions = {
                        new ConditionExpression("gap_scheme", ConditionOperator.Equal, scheme.Id),
                        new ConditionExpression("gap_submissionid", ConditionOperator.Equal, body.SubmissionId)
                    }
                }
            };
            EntityCollection results = service.RetrieveMultiple(query);

            if (results.Entities.Count > 0) {
                var guid = results.Entities[0]["gap_applicationid"];
                context.OutputParameters["Output"] = $"The application already exists (guid={guid}).";
                return;
            }

            Entity application = new Entity("gap_application");
            application["gap_id"] = $"{body.EmailAddress}-{schemeName}";
            application["gap_scheme"] = new EntityReference("gap_scheme", scheme.Id);
            application["gap_submissionid"] = $"{body.SubmissionId}";
            application["gap_submittedon"] = body.SubmittedOn;
            application["gap_gapid"] = $"{body.ApplicationId}";
            application["gap_stage"] = new OptionSetValue(803260000);
            Guid applicationGuid = service.Create(application);

            Entity section0 = new Entity("gap_section");
            section0["gap_application"] = new EntityReference("gap_application", applicationGuid);
            section0["gap_key"] = 0;
            section0["gap_title"] = "Contact Information";
            section0["gap_order"] = 0;
            Guid section0Guid = service.Create(section0);

            Entity response0 = new Entity("gap_response");
            response0["gap_application"] = new EntityReference("gap_application", applicationGuid);
            response0["gap_section"] = new EntityReference("gap_section", section0Guid);
            response0["gap_id"] = "APPLICANT_EMAIL_ADDRESS";
            response0["gap_question"] = "Applicant Email Address";
            response0["gap_answer"] = body.EmailAddress;
            response0["gap_order"] = 0;
            Guid response0Guid = service.Create(response0);

            int questionCounter;
            int sectionCounter;

            sectionCounter = 1;
            foreach (_Section s in body.Sections) {
                Entity section = new Entity("gap_section");
                section["gap_application"] = new EntityReference("gap_application", applicationGuid);
                section["gap_key"] = sectionCounter;
                section["gap_title"] = s.SectionTitle;
                section["gap_order"] = sectionCounter;
                Guid sectionGuid = service.Create(section);

                sectionCounter += 1;
                questionCounter = 1;

                foreach (_Question q in s.Questions) {
                    Entity response = new Entity("gap_response");
                    response["gap_application"] = new EntityReference("gap_application", applicationGuid);
                    response["gap_section"] = new EntityReference("gap_section", sectionGuid);
                    response["gap_id"] = q.QuestionId;
                    response["gap_question"] = q.Question;
                    response["gap_answer"] = q.QuestionResponse.ToString();
                    response["gap_order"] = questionCounter;
                    Guid responseGuid = service.Create(response);

                    questionCounter += 1;
                }
            }

            application = service.Retrieve(application.LogicalName, applicationGuid, new ColumnSet(true));
            application["gap_importcompletedon"] = DateTime.Now;
            service.Update(application);

            context.OutputParameters["Output"] = $"A new application was created (guid={applicationGuid}).";
        }
        catch (Exception ex) {
            throw new InvalidPluginExecutionException($"An error occurred in CreateApplicationAndRelatedRecords: {ex.Message}");
        }
    }
}
