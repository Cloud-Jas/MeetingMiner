using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CodegeistUnleashed.API
{
    public class FxOpenAITrigger
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly ILogger<FxOpenAITrigger> _logger;
        private readonly IConfiguration _configuration;
        public FxOpenAITrigger(ILogger<FxOpenAITrigger> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        private class MessagePayload
        {
            public string[] blobs { get; set; }
            public string webhookUrl { get; set; }
            public string projectKey { get; set; }
            public string projectId { get; set; }

            public string currentIssueKey { get; set; }
        }

        [FunctionName("FxOpenAITrigger")]
        public async Task Run([ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnString")] string message, ILogger log)
        {
            try
            {
                log.LogInformation($"C# ServiceBus queue trigger function processed message: {message}");
                var payload = JsonConvert.DeserializeObject<MessagePayload>(message);
                var handler = new HttpClientHandler()
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback =
                            (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
                };
                using (var client = new HttpClient(handler))
                {
                    var requestBody = new { blobs = payload.blobs };
                    string apiKey = _configuration.GetValue<string>("PromptFlowApiKey");
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    client.BaseAddress = new Uri(_configuration.GetValue<string>("PromptFlowUrl"));
                    client.Timeout = TimeSpan.FromMinutes(5);

                    var content = new StringContent(JsonConvert.SerializeObject(requestBody));
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    content.Headers.Add("azureml-model-deployment", _configuration.GetValue<string>("PromptFlowDeploymentName"));
                    HttpResponseMessage response = await client.PostAsync("", content);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        log.LogInformation("Result: {0}", result);
                        var requestData = new
                        {
                            projectKey = payload.projectKey,
                            projectId = payload.projectId,
                            currentIssueKey = payload.currentIssueKey,
                            stories = result
                        };
                        string jsonRequestData = JsonConvert.SerializeObject(requestData);
                        log.LogInformation("Webhook req payload: {0}", jsonRequestData);
                        await CallJiraWebhookTrigger(payload.webhookUrl, jsonRequestData);
                    }
                    else
                    {
                        log.LogInformation(string.Format("The request failed with status code: {0}", response.StatusCode));
                        log.LogInformation(response.Headers.ToString());
                        string responseContent = await response.Content.ReadAsStringAsync();
                        log.LogInformation(responseContent);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"excetion message:{ex.Message} stack trace: {ex.StackTrace}");
            }
        }
        private static async Task CallJiraWebhookTrigger(string webhookUrl, string requestBody)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(webhookUrl, new StringContent(requestBody));

                if (response.IsSuccessStatusCode)
                {

                }
                else
                {

                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
