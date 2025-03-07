using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using ServerlessRAG.DocumentProcessing.Activities;
using ServerlessRAG.Unstructured.Models;
using ServerlessRAG.VectorEmbedding.Models;

namespace ServerlessRAG.DocumentProcessing.Orchestrators
{
    public static class ProcessDocumentPdfSplitterOrchestrator
    {
        [Function(nameof(ProcessDocumentPdfSplitterOrchestrator))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(ProcessDocumentPdfSplitterOrchestrator));
            var options = TaskOptions.FromRetryPolicy(new RetryPolicy(
                maxNumberOfAttempts: 3,
                firstRetryInterval: TimeSpan.FromSeconds(5)));

            // Retrieve the uploaded blob input.
            var uploadInput = context.GetInput<UploadBlobInput>();

            // Generate a replay-safe GUID for this document processing job.
            string documentProcessorJobId = context.NewGuid().ToString();
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

            //////////////////////// Preprocess PDF documents
            Dictionary<int, List<Element>> sectionIngestResults = null;

            // Read configuration for pages per section, defaulting to 25.
            int pagesPerSection = 25;
            string pdfPagesFromEnv = Environment.GetEnvironmentVariable("PdfPagesPerSection");
            if (!string.IsNullOrEmpty(pdfPagesFromEnv) && int.TryParse(pdfPagesFromEnv, out int configPages))
            {
                pagesPerSection = configPages;
            }

            // Call the preprocessing activity to split the PDF into sections.
            var preprocessInput = new PreprocessPdfActivityInput
            {
                OrgId = uploadInput.OrgId,
                FileName = uploadInput.FileName,
                FileBytes = uploadInput.FileBytes,
                DocumentProcessorJobId = documentProcessorJobId,
                PagesPerSection = pagesPerSection
            };

            List<PreprocessPdfSection> sections = await context.CallActivityAsync<List<PreprocessPdfSection>>(
                nameof(PreprocessPdfActivity), preprocessInput, options);
            outputs.Add($"PDF split into {sections.Count} sections.");

            for (int i = 0; i < sections.Count; i++)
            {
                int sectionIndex = i + 1;
                var uploadSectionInput = new UploadPdfSectionActivityInput
                {
                    OrgId = uploadInput.OrgId,
                    DocumentProcessorJobId = documentProcessorJobId,
                    SectionIndex = sectionIndex,
                    SectionBytes = sections[i].SectionBytes,
                    FileName = uploadInput.FileName  // or modify if you wish to include "Section" prefix
                };

                string sectionUploadResult = await context.CallActivityAsync<string>(
                    nameof(UploadPdfSectionActivity),
                    uploadSectionInput,
                    options);
                outputs.Add($"Section {sectionIndex}: Uploaded pdf section blob at {sectionUploadResult}");

                // a. Ingest the section -> get elements
                var ingestInput = new IngestDocumentActivityInput
                {
                    OrgId = uploadInput.OrgId,
                    FileName = $"Section-{sectionIndex}-{uploadInput.FileName}",
                    FileBytes = sections[i].SectionBytes,
                    DocumentProcessorJobId = documentProcessorJobId,
                    SectionIndex = sectionIndex,
                    Strategy = uploadInput.IngestionStrategy
                };

                List<Element> elements = await context.CallActivityAsync<List<Element>>(
                    nameof(IngestDocumentActivity),
                    ingestInput,
                    options);
                outputs.Add($"Section {sectionIndex}: Ingested {elements.Count} elements.");

                // b. Chunk the elements
                var chunkInput = new ChunkElementsActivityInput
                {
                    OrgId = uploadInput.OrgId,
                    DocumentProcessorJobId = documentProcessorJobId,
                    Options = uploadInput.ChunkingOptions,
                    Elements = elements
                };

                List<Chunk> chunks = await context.CallActivityAsync<List<Chunk>>(
                    nameof(ChunkElementsActivity),
                    chunkInput,
                    options);
                outputs.Add($"Section {sectionIndex}: Created {chunks.Count} chunks.");

                // c. Fan-out: Upload each chunk with section indexing.
                var uploadChunkTasks = new List<Task<string>>();
                int chunkIndex = 1;
                foreach (var chunk in chunks)
                {
                    var uploadChunkInput = new UploadChunkActivityInput
                    {
                        OrgId = uploadInput.OrgId,
                        DocumentProcessorJobId = documentProcessorJobId,
                        Index = chunkIndex,
                        Chunk = chunk,
                        SectionIndex = sectionIndex
                    };
                    uploadChunkTasks.Add(context.CallActivityAsync<string>(
                        nameof(UploadChunkActivity),
                        uploadChunkInput,
                        options));
                    chunkIndex++;
                }
                string[] uploadChunkResults = await Task.WhenAll(uploadChunkTasks);
                outputs.Add($"Section {sectionIndex}: Uploaded {uploadChunkResults.Length} chunks.");

                // d. Get embeddings for the chunks.
                var embedInput = new EmbedChunksActivityInput
                {
                    Chunks = chunks,
                    Model = uploadInput.EmbeddingModel,
                    Platform = uploadInput.EmbeddingPlatform,
                    InputType = "document"
                };

                List<ChunkWithEmbedding> chunksWithEmbedding = await context.CallActivityAsync<List<ChunkWithEmbedding>>(
                    nameof(EmbedChunksActivity),
                    embedInput,
                    options);
                outputs.Add($"Section {sectionIndex}: Embedded {chunksWithEmbedding.Count} chunks.");


                // Set the file name in each chunk’s metadata.
                foreach (var chunk in chunksWithEmbedding)
                {                
                    chunk.FileName = uploadInput.FileName;
                }

                // e. Fan-out: Upload each chunk embedding with section indexing.
                var uploadEmbeddingTasks = new List<Task<string>>();
                foreach (var chunkEmbedding in chunksWithEmbedding)
                {
                    var uploadEmbeddingInput = new UploadChunkEmbeddingActivityInput
                    {
                        OrgId = uploadInput.OrgId,
                        DocumentProcessorJobId = documentProcessorJobId,
                        ChunkWithEmbedding = chunkEmbedding,
                        SectionIndex = sectionIndex  // NEW: indicate a section upload
                    };
                    uploadEmbeddingTasks.Add(context.CallActivityAsync<string>(
                        nameof(UploadChunkEmbeddingActivity),
                        uploadEmbeddingInput,
                        options));
                }
                string[] uploadEmbeddingResults = await Task.WhenAll(uploadEmbeddingTasks);
                outputs.Add($"Section {sectionIndex}: Uploaded {uploadEmbeddingResults.Length} chunk embeddings.");

                // f. Bulk save embeddings to the database.
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
                    outputs.Add($"Section {sectionIndex}: {bulkUploadResult}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Section {sectionIndex}: Error saving embeddings to database.");
                    outputs.Add($"Section {sectionIndex}: Error saving embeddings to database.");
                }
            }
            return outputs;
        }
    }
}
