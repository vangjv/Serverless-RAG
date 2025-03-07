using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public class UploadPdfSectionActivityInput
    {
        public string OrgId { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public int SectionIndex { get; set; }
        public byte[] SectionBytes { get; set; }
        public string FileName { get; set; }
    }

    public static class UploadPdfSectionActivity
    {
        [Function("UploadPdfSectionActivity")]
        public static async Task<string> Run(
            [ActivityTrigger] UploadPdfSectionActivityInput input,
            FunctionContext context)
        {
            var logger = context.GetLogger("UploadPdfSectionActivity");
            logger.LogInformation($"Uploading PDF section {input.SectionIndex} for job '{input.DocumentProcessorJobId}'.");

            string connectionString = Environment.GetEnvironmentVariable("BlobStorageConnString");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("organizations");
            containerClient.CreateIfNotExists();

            // Build blob path:
            // /organizations/{orgId}/documentprocessing/{documentProcessorJobId}/sections/{sectionIndex}/{FileName}
            string blobPath = $"{input.OrgId}/documentprocessing/{input.DocumentProcessorJobId}/sections/{input.SectionIndex}/{input.FileName}";
            var blobClient = containerClient.GetBlobClient(blobPath);

            using (var stream = new MemoryStream(input.SectionBytes))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            logger.LogInformation($"Uploaded PDF section to blob path: {blobPath}");
            return blobPath;
        }
    }
}
