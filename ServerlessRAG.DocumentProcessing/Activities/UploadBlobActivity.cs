using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class UploadBlobActivity
    {
        [Function("UploadBlobActivity")]
        public static string Run(
            [ActivityTrigger] UploadBlobInput input,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadBlobActivity");
            logger.LogInformation($"Uploading blob for org '{input.OrgId}' and file '{input.FileName}'.");

            string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(input.ContainerName);
            containerClient.CreateIfNotExists();

            string blobPath = $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/upload/{input.FileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            using (var stream = new MemoryStream(input.FileBytes))
            {
                blobClient.Upload(stream, overwrite: true);
            }
            return $"Uploaded blob '{blobPath}' successfully.";
        }
    }
}
