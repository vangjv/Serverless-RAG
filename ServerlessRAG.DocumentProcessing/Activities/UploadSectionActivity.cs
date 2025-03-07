using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class UploadSectionActivity
    {
        [Function("UploadSectionActivity")]
        public static string Run(
            [ActivityTrigger] UploadSectionActivityInput input,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadSectionActivity");
            logger.LogInformation($"Uploading blob for org '{input.OrgId}' and file '{input.FileName}'.");

            string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(input.ContainerName);
            containerClient.CreateIfNotExists();

            string blobPath = $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/sections/{input.Index}/upload/{input.Index}-{input.FileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            using (var stream = new MemoryStream(input.FileBytes))
            {
                blobClient.Upload(stream, overwrite: true);
            }
            return $"Uploaded blob '{blobPath}' successfully.";
        }
    }

    public class UploadSectionActivityInput
    {
        public string ContainerName { get; set; }
        public string FileName { get; set; }
        public string OrgId { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public int Index { get; set; }
        public byte[] FileBytes { get; set; }
    }
}
