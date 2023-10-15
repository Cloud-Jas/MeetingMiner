using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace CodegeistUnleashed.API
{
    public class FxPushMessageToQueue
    {
        private readonly ILogger<FxPushMessageToQueue> _logger;                
        public FxPushMessageToQueue(ILogger<FxPushMessageToQueue> log)
        {
            _logger = log;            
        }

        [FunctionName("FxPushMessageToQueue")]
        [OpenApiOperation(operationId: "PushMessageToQueue", tags: new[] { "ServiceBus" })]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Description = "The JSON payload to send to the Service Bus queue.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Internal Server Error")]
        public async Task<IActionResult> PushMessageToQueue(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        [ServiceBus("%ServiceBusQueueName%", Connection = "ServiceBusConnString")] IAsyncCollector<string> messageCollector,
        ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var messagePayload = requestBody;

                await messageCollector.AddAsync(messagePayload);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError($"Error pushing message to service bus: {ex.Message}");
                return new ObjectResult("Internal Server Error") { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }
    }
}