using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using ServerlessRAG.DocumentProcessing.Activities;
using ServerlessRAG.Unstructured.Models;
using ServerlessRAG.VectorEmbedding.Models;
namespace ServerlessRAG.DocumentProcessing.Orchestrators
{
    public static class ProcessDocumentOrchestrator
    {
        [Function(nameof(ProcessDocumentOrchestrator))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(ProcessDocumentOrchestrator));
            var options = TaskOptions.FromRetryPolicy(new RetryPolicy(
                maxNumberOfAttempts: 3,
                firstRetryInterval: TimeSpan.FromSeconds(5)));

            // Retrieve the uploaded blob input.
            var uploadInput = context.GetInput<UploadBlobInput>();

            // Generate a replay-safe GUID for this document processing job.
            //string documentProcessorJobId = context.NewGuid().ToString();
            string timestamp = context.CurrentUtcDateTime.ToString("yyyyMMdd_HHmmssfff"); // UTC datetime with milliseconds
            string documentProcessorJobId = timestamp + "_" + uploadInput.FileName;
            logger.BeginScope("DocumentProcessorJobId", documentProcessorJobId);
            // Set the job ID on the upload input.
            uploadInput.DocumentProcessorJobId = documentProcessorJobId;

            // Prepare an output collection.
            var outputs = new List<string>();

            //////////////////////// upload the file to blobstorage
            if (uploadInput != null && uploadInput.FileBytes != null)
            {
                string uploadResult = await context.CallActivityAsync<string>(
                    nameof(UploadBlobActivity), uploadInput, options);
                outputs.Add(uploadResult);
            }

            //////////////////////// Call the ingestion activity to get elements from document
            var ingestInput = new IngestDocumentActivityInput
            {
                OrgId = uploadInput.OrgId,
                FileName = uploadInput.FileName,
                FileBytes = uploadInput.FileBytes,
                DocumentProcessorJobId = documentProcessorJobId,
                Strategy = uploadInput.IngestionStrategy
            };
            List<Element> ingestResult = await context.CallActivityAsync<List<Element>>(
                nameof(IngestDocumentActivity), ingestInput, options);
            outputs.Add($"Elements returned from Unstructured: {ingestResult.Count}");

            //////////////////////// Create chunks from elements
            ChunkElementsActivityInput chunkInput = new ChunkElementsActivityInput
            {
                OrgId = uploadInput.OrgId,
                DocumentProcessorJobId = documentProcessorJobId,
                Options = uploadInput.ChunkingOptions,
                Elements = ingestResult
            };
            List<Chunk> chunks = await context.CallActivityAsync<List<Chunk>>(
                nameof(ChunkElementsActivity), chunkInput, options);
            outputs.Add($"Created {chunks.Count} chunks.");

            //////////////////////// Fan out and upload each chunk.
            var uploadChunkTasks = new List<Task<string>>();
            int index = 1;
            foreach (var chunk in chunks)
            {
                var uploadChunkInput = new UploadChunkActivityInput
                {
                    OrgId = uploadInput.OrgId,
                    DocumentProcessorJobId = documentProcessorJobId,
                    Index = index,
                    Chunk = chunk
                };
                uploadChunkTasks.Add(context.CallActivityAsync<string>(
                    nameof(UploadChunkActivity), uploadChunkInput, options));
                index++;
            }

            // Wait for all upload tasks to complete.
            string[] uploadResults = await Task.WhenAll(uploadChunkTasks);
            outputs.Add($"Uploaded {uploadResults.Length} chunks.");

            //////////////////////// Call the new EmbedChunksActivity to generate embeddings for each chunk.
            var embedInput = new EmbedChunksActivityInput
            {
                Chunks = chunks,
                Model = uploadInput.EmbeddingModel,
                Platform = uploadInput.EmbeddingPlatform,
                InputType = "document"
            };
            List<ChunkWithEmbedding> chunksWithEmbedding =
                await context.CallActivityAsync<List<ChunkWithEmbedding>>(
                    nameof(EmbedChunksActivity),
                    embedInput,
                    options);
            outputs.Add($"Embedded {chunksWithEmbedding.Count} chunks.");

            // Set the file name in each chunk’s metadata.
            foreach (var chunk in chunksWithEmbedding)
            {
                chunk.FileName = uploadInput.FileName;
            }

            //////////////////////// Fan-out: Upload each chunkWithEmbedding individually.
            var uploadEmbeddingTasks = new List<Task<string>>();
            foreach (var chunkEmbedding in chunksWithEmbedding)
            {
                var uploadEmbeddingInput = new UploadChunkEmbeddingActivityInput
                {
                    OrgId = uploadInput.OrgId,
                    DocumentProcessorJobId = documentProcessorJobId,
                    ChunkWithEmbedding = chunkEmbedding
                };
                uploadEmbeddingTasks.Add(
                    context.CallActivityAsync<string>(
                        nameof(UploadChunkEmbeddingActivity),
                        uploadEmbeddingInput,
                        options));
            }
            string[] uploadEmbeddingResults = await Task.WhenAll(uploadEmbeddingTasks);
            outputs.Add($"Uploaded {uploadEmbeddingResults.Length} chunk embeddings.");

            //////////////////////// Bulk save all embeddings to db
            var bulkSaveInput = new SaveEmbeddingsToDbInput
            {
                OrgId = uploadInput.OrgId,
                ChunksWithEmbedding = chunksWithEmbedding
            };

            try
            {
               string bulkUploadResult = await context.CallActivityAsync<string>(
               nameof(SaveEmbeddingsToDbActivity),
               bulkSaveInput,
               options);
                outputs.Add(bulkUploadResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving embeddings to database.");
                outputs.Add("Error saving embeddings to database.");
            }

            return outputs;
        }
    }
}
