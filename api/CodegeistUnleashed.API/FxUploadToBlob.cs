using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace CodegeistUnleashed.API
{
    public class FxUploadToBlob
    {
        private readonly ILogger<FxUploadToBlob> _logger;
        private readonly IConfiguration _configruation;
        private readonly BlobServiceClient _client;
        private readonly string _containerName;
        public FxUploadToBlob(ILogger<FxUploadToBlob> log,IConfiguration configuration)
        {
            _logger = log;
            _configruation = configuration;
            string azureStorageConnectionString = configuration.GetValue<string>("BlobConnString");
            _containerName = configuration.GetValue<string>("BlobContainerName");
            _client = new BlobServiceClient(azureStorageConnectionString);
        }
        public class AttachmentData
        {
            public string Data { get; set; }
            public string Filename { get; set; }
        }

        [FunctionName("FxUploadToBlob")]
        [OpenApiOperation(operationId: "UploadBlob", tags: new[] { "Blob" })]
        [OpenApiRequestBody(contentType: "multipart/form-data", bodyType: typeof(HttpRequest), Required = true, Description = "File to upload")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "Successful response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "Bad request")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "Internal server error")]
        public async Task<IActionResult> UploadToBlob(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                AttachmentData attachmentData = JsonConvert.DeserializeObject<AttachmentData>(requestBody);

                log.LogInformation("Attachment data", requestBody);
                if (attachmentData == null || string.IsNullOrEmpty(attachmentData.Data) || string.IsNullOrEmpty(attachmentData.Filename))
                {
                    return new BadRequestObjectResult("Invalid or missing attachment data.");
                }
                
                byte[] dataBytes = Encoding.UTF8.GetBytes(attachmentData.Data);                                
                BlobContainerClient containerClient = _client.GetBlobContainerClient(_containerName);
                BlobClient blobClient = containerClient.GetBlobClient(attachmentData.Filename);
                await blobClient.UploadAsync(new MemoryStream(dataBytes), true);

                return new OkObjectResult($"Blob uploaded successfully. Blob name: {attachmentData.Filename}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error uploading blob: {ex.Message}");
                return new StatusCodeResult(500);
            }
        }
    }
}