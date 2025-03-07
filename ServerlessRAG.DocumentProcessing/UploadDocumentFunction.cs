using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace ServerlessRAG.DocumentProcessing
{
    public class UploadFileFunction
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger _logger;

        public UploadFileFunction(BlobServiceClient blobServiceClient, ILoggerFactory loggerFactory)
        {
            _blobServiceClient = blobServiceClient;
            _logger = loggerFactory.CreateLogger<UploadFileFunction>();
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequestData req)
        {
            // Verify the Content-Type header exists
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                var missingResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                missingResponse.WriteString("Missing Content-Type header.");
                return missingResponse;
            }

            var contentType = contentTypeValues.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(contentType))
            {
                var invalidResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                invalidResponse.WriteString("Invalid Content-Type header.");
                return invalidResponse;
            }

            // Parse the boundary from the Content-Type header
            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
            {
                var boundaryResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                boundaryResponse.WriteString("Could not determine boundary from Content-Type header.");
                return boundaryResponse;
            }

            // Prepare the multipart reader
            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection section = await reader.ReadNextSectionAsync();

            // Variables to hold file content, file name, and orgId
            MemoryStream fileStream = null;
            string fileName = null;
            string orgId = null;

            // Iterate over each section in the multipart form-data
            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    // Get the field name (if any) without quotes.
                    var fieldName = contentDisposition.Name.Value?.Trim('"');

                    // Check if this section is a file upload (has a file name)
                    if ((contentDisposition.FileName.HasValue && !string.IsNullOrEmpty(contentDisposition.FileName.Value)) ||
                        (contentDisposition.FileNameStar.HasValue && !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)))
                    {
                        // Capture the file name
                        fileName = contentDisposition.FileName.HasValue
                            ? contentDisposition.FileName.Value
                            : contentDisposition.FileNameStar.Value;
                        _logger.LogInformation($"Received file: {fileName}");

                        // Buffer the file content in memory.
                        fileStream = new MemoryStream();
                        await section.Body.CopyToAsync(fileStream);
                        fileStream.Position = 0;
                    }
                    // Check if this section is the orgId field.
                    else if (!string.IsNullOrEmpty(fieldName) &&
                             fieldName.Equals("orgId", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamReader = new StreamReader(section.Body))
                        {
                            orgId = (await streamReader.ReadToEndAsync()).Trim();
                            orgId = orgId.Trim('"');
                        }
                        _logger.LogInformation($"Parsed orgId: {orgId}");
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            if (fileStream == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                notFoundResponse.WriteString("No file found in the request.");
                return notFoundResponse;
            }

            // Build the blob name. If orgId is provided, use it as a virtual folder prefix.
            string blobName = !string.IsNullOrEmpty(orgId)
                ? $"{orgId}/uploads/{fileName}"
                : $"uploads/{fileName}";

            // Upload the file to Azure Blob Storage
            string containerName = "organizations"; // Change this if needed.
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("File uploaded successfully.");
            return response;
        }
    }
}
