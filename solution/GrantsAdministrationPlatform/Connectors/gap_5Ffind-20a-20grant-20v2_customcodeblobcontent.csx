public class Script : ScriptBase {
    public override async Task<HttpResponseMessage> ExecuteAsync() {
        try {
            var response = await this.Context.SendAsync(this.Context.Request, this.CancellationToken);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            
            if (response.StatusCode != HttpStatusCode.OK) {
                response.Content = CreateJsonContent(
                    JsonConvert.SerializeObject(
                        new { Applications = new List<object>() }
                    )
                );
                return response;
            }
            else {
                var submissions = new List<object>();
                foreach (var application in json["applications"]) {
                    foreach (var submission in application["submissions"]) {
                        var sections = new List<object>();
                        foreach (var section in submission["sections"]) {
                            var questions = new List<object>();
                            foreach (var question in section["questions"]) {
                                questions.Add(
                                    new {
                                        QuestionId = question["questionId"].ToString(), 
                                        Question = question["questionTitle"].ToString(), 
                                        QuestionResponse = question["questionResponse"] .ToString()
                                    }
                                );
                            }
                            sections.Add( 
                                new {
                                    SectionId = section["sectionId"].ToString(), 
                                    SectionTitle = section["sectionTitle"].ToString(), 
                                    Questions = questions 
                                }
                            );
                        }
                        submissions.Add(
                            new {
                                ApplicationId = submission["gapId"].ToString(),
                                SubmissionId = submission["submissionId"].ToString(),
                                ApplicationFormName = application["applicationFormName"].ToString(),
                                EmailAddress = submission["grantApplicantEmailAddress"].ToString(),
                                SubmittedOn = submission["submittedTimeStamp"].ToString(),
                                Sections = sections
                            }
                        );
                    }
                }
                response.Content = CreateJsonContent(
                    JsonConvert.SerializeObject(
                        new { Applications = submissions }
                    )
                );
                return response;
            }
        }
        catch (Exception ex) {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            response.Content = CreateJsonContent(
                JsonConvert.SerializeObject(
                    new { Applications = new List<object>() }
                )
            );
            return response;  
        }
    }
}