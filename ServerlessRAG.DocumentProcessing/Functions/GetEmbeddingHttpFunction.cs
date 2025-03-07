using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServerlessRAG.VectorEmbedding.Services;

namespace ServerlessRAG.DocumentProcessing.Functions
{
    public class GetEmbeddingHttpFunction
    {
        private readonly VoyageEmbeddingService _embeddingService;

        // Inject IHttpClientFactory (make sure your Function App is configured for DI)
        public GetEmbeddingHttpFunction(IHttpClientFactory httpClientFactory)
        {
            _embeddingService = new VoyageEmbeddingService(httpClientFactory.CreateClient());
        }

        [Function("Embed")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "embed")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("Embed");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Expecting a JSON payload like: { "text": "your text content" }
            var data = JsonSerializer.Deserialize<EmbeddingRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data == null || string.IsNullOrWhiteSpace(data.Text))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid input. Please pass a JSON object with property 'text'.");
                return badResponse;
            }

            var texts = new List<string> { data.Text };

            // Call the embedding service with default model and input type
            try
            {
                var embeddings = await _embeddingService.GetEmbeddingsAsync(texts, "voyage-3-lite", "document");

                if (embeddings == null || embeddings.Count == 0)
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                    await errorResponse.WriteStringAsync("No embedding obtained from the service.");
                    return errorResponse;
                }

                // Return the first embedding from the result list
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                string jsonResponse = JsonSerializer.Serialize(embeddings[0], new JsonSerializerOptions { WriteIndented = true });
                await response.WriteStringAsync(jsonResponse);

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while retrieving embedding.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An error occurred while processing the embedding request.");
                return errorResponse;
            }
        }
    }

    public class EmbeddingRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
