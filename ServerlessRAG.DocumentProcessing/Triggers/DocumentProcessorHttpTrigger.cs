using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Text.Json;
using ServerlessRAG.DocumentProcessing.Models;
using ServerlessRAG.DocumentProcessing.Orchestrators;
using Microsoft.AspNetCore.WebUtilities;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ServerlessRAG.DocumentProcessing.Triggers
{
    public static class DocumentProcessorHttpTrigger
    {
        [Function("DocumentProcessor")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("DocumentProcessor");

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

            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
            {
                var boundaryResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                boundaryResponse.WriteString("Could not determine boundary from Content-Type header.");
                return boundaryResponse;
            }

            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection section = await reader.ReadNextSectionAsync();

            MemoryStream fileStream = null;
            string fileName = null;
            string orgId = null;
            string ingestionStrategy = null;
            ChunkingOptions chunkingOptions = null;
            string embeddingPlatform = null;
            string embeddingModel = null;


            while (section != null)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    var fieldName = contentDisposition.Name.Value?.Trim('"');

                    if ((contentDisposition.FileName.HasValue && !string.IsNullOrEmpty(contentDisposition.FileName.Value)) ||
                        (contentDisposition.FileNameStar.HasValue && !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)))
                    {
                        fileName = contentDisposition.FileName.HasValue
                            ? contentDisposition.FileName.Value
                            : contentDisposition.FileNameStar.Value;
                        logger.LogInformation($"Received file: {fileName}");

                        fileStream = new MemoryStream();
                        await section.Body.CopyToAsync(fileStream);
                        fileStream.Position = 0;
                    }
                    else if (!string.IsNullOrEmpty(fieldName) &&
                             fieldName.Equals("orgId", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamReader = new StreamReader(section.Body))
                        {
                            orgId = (await streamReader.ReadToEndAsync()).Trim();
                            orgId = orgId.Trim('"');
                        }
                        logger.LogInformation($"Parsed orgId: {orgId}");
                    }
                    else if (!string.IsNullOrEmpty(fieldName) &&
                             fieldName.Equals("ingestionStrategy", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamReader = new StreamReader(section.Body))
                        {
                            ingestionStrategy = (await streamReader.ReadToEndAsync()).Trim().Trim('"');
                        }
                        logger.LogInformation($"Parsed ingestionStrategy: {ingestionStrategy}");
                    }
                    else if (!string.IsNullOrEmpty(fieldName) &&
                        fieldName.Equals("chunkingOptions", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamReader = new StreamReader(section.Body))
                        {
                            var chunkOptionsJson = await streamReader.ReadToEndAsync();
                            if (!string.IsNullOrWhiteSpace(chunkOptionsJson))
                            {
                                chunkingOptions = JsonSerializer.Deserialize<ChunkingOptions>(chunkOptionsJson);
                            }
                        }
                        logger.LogInformation("Parsed chunkingOptions.");
                    }

                    else if (!string.IsNullOrEmpty(fieldName) &&
                             fieldName.Equals("embeddingModel", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamReader = new StreamReader(section.Body))
                        {
                            embeddingModel = (await streamReader.ReadToEndAsync()).Trim().Trim('"');
                        }
                        logger.LogInformation($"Parsed embeddingModel: {embeddingModel}");
                    }
                    else if (!string.IsNullOrEmpty(fieldName) &&
                             fieldName.Equals("embeddingPlatform", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var streamReader = new StreamReader(section.Body))
                        {
                            embeddingPlatform = (await streamReader.ReadToEndAsync()).Trim().Trim('"');
                        }
                        logger.LogInformation($"Parsed embeddingPlatform: {embeddingPlatform}");
                    }
                    section = await reader.ReadNextSectionAsync();
                }
            }

            if (fileStream == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                notFoundResponse.WriteString("No file found in the request.");
                return notFoundResponse;
            }

            var fileBytes = fileStream.ToArray();
            var uploadInput = new UploadBlobInput
            {
                ContainerName = "organizations",
                OrgId = orgId,
                FileName = fileName,
                FileBytes = fileBytes,
                IngestionStrategy = ingestionStrategy,
                ChunkingOptions = chunkingOptions,
                EmbeddingModel = embeddingModel,
                EmbeddingPlatform = embeddingPlatform
            };

            // Determine which orchestrator to use based on PDF page count.
            string orchestrationName = nameof(ProcessDocumentOrchestrator);
            if (uploadInput.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                // Open the PDF from the file bytes.
                using (var pdfStream = new MemoryStream(uploadInput.FileBytes))
                {
                    PdfDocument pdf = PdfReader.Open(pdfStream, PdfDocumentOpenMode.ReadOnly);
                    int pageCount = pdf.Pages.Count;

                    // Read environment variable for the pages threshold.
                    string pdfPagesEnv = Environment.GetEnvironmentVariable("PdfPagesPerSection");
                    if (!string.IsNullOrEmpty(pdfPagesEnv) &&
                        int.TryParse(pdfPagesEnv, out int pdfPagesThreshold) &&
                        pageCount > pdfPagesThreshold)
                    {
                        orchestrationName = nameof(ProcessDocumentPdfSplitterOrchestrator);
                    }
                }
            }

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                orchestrationName,
                uploadInput);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
