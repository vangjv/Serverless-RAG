using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.Unstructured;
using ServerlessRAG.Unstructured.Models;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class IngestDocumentActivity
    {
        [Function("IngestDocumentActivity")]
        public static async Task<List<Element>> Run(
            [ActivityTrigger] IngestDocumentActivityInput input,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("IngestDocumentActivity");
            logger.LogInformation($"Starting ingestion for file '{input.FileName}' for org '{input.OrgId}'.");

            // Get the Blob Storage connection string.
            string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");

            // Create a BlobServiceClient and get the "organizations" container.
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("organizations");

            // Build a stream from the file bytes in the input.
            using var fileStream = new MemoryStream(input.FileBytes);

            // Retrieve IIngestionService from the DI container.
            var ingestionService = executionContext.InstanceServices.GetService(typeof(IIngestionService)) as IIngestionService;
            if (ingestionService == null)
            {
                throw new Exception("IIngestionService not available in DI container.");
            }

            // Ingest the document.
            var elements = await ingestionService.IngestDocumentAsync(fileStream, input.FileName, input.Strategy);

            // Serialize the output elements to JSON.
            string jsonString = JsonSerializer.Serialize(elements, new JsonSerializerOptions { WriteIndented = true });
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            using var jsonStream = new MemoryStream(jsonBytes);

            // Build a blob path using a timestamp.
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string jsonBlobPath = (input.SectionIndex.HasValue)
                ? $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/sections/{input.SectionIndex}/elements/{input.FileName}_{timestamp}.json"
                : $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/elements/{input.FileName}_{timestamp}.json";
            var jsonBlobClient = containerClient.GetBlobClient(jsonBlobPath);

            // Upload the JSON output.
            await jsonBlobClient.UploadAsync(jsonStream, overwrite: true);

            logger.LogInformation($"Saved ingestion output to blob '{jsonBlobPath}'.");
            return elements;
        }
    }

    // Input definition for the ingestion activity.
    public class IngestDocumentActivityInput
    {
        public string OrgId { get; set; }
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public string Strategy { get; set; }
        public int? SectionIndex { get; set; }
    }
}
