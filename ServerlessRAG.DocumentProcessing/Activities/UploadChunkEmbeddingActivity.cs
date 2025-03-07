using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.VectorEmbedding.Models;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public class UploadChunkEmbeddingActivityInput
    {
        public string OrgId { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public ChunkWithEmbedding ChunkWithEmbedding { get; set; }
        // NEW:
        public int? SectionIndex { get; set; }
    }

    public static class UploadChunkEmbeddingActivity
    {
        [Function("UploadChunkEmbeddingActivity")]
        public static async Task<string> Run(
            [ActivityTrigger] UploadChunkEmbeddingActivityInput input,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadChunkEmbeddingActivity");
            logger.LogInformation($"Uploading chunk with embedding id {input.ChunkWithEmbedding.Id} for job '{input.DocumentProcessorJobId}'.");

            string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");
            var blobServiceClient = new BlobServiceClient(connectionString);
            // Use the same container as other activities, i.e. "organizations"
            var containerClient = blobServiceClient.GetBlobContainerClient("organizations");

            // Serialize the chunk with embedding to JSON.
            string chunkJson = JsonSerializer.Serialize(input.ChunkWithEmbedding, new JsonSerializerOptions { WriteIndented = true });
            byte[] chunkBytes = Encoding.UTF8.GetBytes(chunkJson);
            using var chunkStream = new MemoryStream(chunkBytes);

            // Build blob path:
            // organizations/{OrgId}/documentprocessing/{DocumentProcessorJobId}/chunksWithEmbeddings/{chunkid}.json
            string blobPath = (input.SectionIndex.HasValue)
                ? $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/sections/{input.SectionIndex}/chunksWithEmbeddings/{input.ChunkWithEmbedding.Id}.json"
                : $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/chunksWithEmbeddings/{input.ChunkWithEmbedding.Id}.json";
            var blobClient = containerClient.GetBlobClient(blobPath);

            await blobClient.UploadAsync(chunkStream, overwrite: true);
            logger.LogInformation($"Uploaded chunk with embedding to blob path: {blobPath}");

            return blobPath;
        }
    }
}
