using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.Unstructured;

namespace UnstructuredDocumentIngest
{
    public class IngestDocumentFunction
    {
        private readonly ILogger<IngestDocumentFunction> _logger;
        private readonly IIngestionService _ingestionService;

        public IngestDocumentFunction(ILogger<IngestDocumentFunction> logger, IIngestionService ingestionService)
        {
            _logger = logger;
            _ingestionService = ingestionService;
        }

        [Function("IngestDocument")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ingest")] HttpRequest req)
        {
            _logger.LogInformation("HTTP trigger function processed a request.");

            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult("Request must be form-data.");
            }

            var form = await req.ReadFormAsync();
            var file = form.Files["file"];
            var ingestionStrategy = form["ingestionStrategy"].ToString();
            if (file == null)
            {
                return new BadRequestObjectResult("File is required.");
            }

            // Use the file's built-in stream
            await using var fileStream = file.OpenReadStream();

            try
            {
                var elements = await _ingestionService.IngestDocumentAsync(fileStream, file.FileName, ingestionStrategy ?? "fast");
                return new OkObjectResult(elements);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error ingesting document.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
