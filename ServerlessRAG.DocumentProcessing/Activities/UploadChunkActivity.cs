using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.Unstructured.Models;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class UploadChunkActivity
    {
        [Function("UploadChunkActivity")]
        public static async Task<string> Run(
            [ActivityTrigger] UploadChunkActivityInput input,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadChunkActivity");
            logger.LogInformation($"Uploading chunk index {input.Index} for job '{input.DocumentProcessorJobId}'.");

            string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("organizations");

            // Serialize the chunk to JSON.
            string chunkJson = JsonSerializer.Serialize(input.Chunk, new JsonSerializerOptions { WriteIndented = true });
            byte[] chunkBytes = Encoding.UTF8.GetBytes(chunkJson);
            using var chunkStream = new MemoryStream(chunkBytes);

            // Build blob path: organizations/{OrgId}/documentprocessing/{DocumentProcessorJobId}/chunks/{Index}.json
            string chunkBlobPath = (input.SectionIndex.HasValue)
                ? $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/sections/{input.SectionIndex}/chunks/{input.Index}.json"
                : $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/chunks/{input.Index}.json";
            var chunkBlobClient = containerClient.GetBlobClient(chunkBlobPath);

            await chunkBlobClient.UploadAsync(chunkStream, overwrite: true);

            logger.LogInformation($"Uploaded chunk to blob path: {chunkBlobPath}");
            return chunkBlobPath;
        }

    }

    public class UploadChunkActivityInput
    {
        public string OrgId { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public int Index { get; set; }
        public Chunk Chunk { get; set; }
        public int? SectionIndex { get; set; }
    }
}
