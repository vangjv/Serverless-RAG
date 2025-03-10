using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServerlessRAG.VectorEmbedding.Services;
using System.Text.Json.Serialization;
using ServerlessRAG.VectorEmbedding.Models;

namespace ServerlessRAG.DocumentProcessing.Functions
{
    public class VectorSearchHttpFunction
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VectorSearchHttpFunction(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Function("VectorSearchHttpFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "vectorsearch")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("VectorSearchHttpFunction");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            VectorSearchRequest searchRequest;
            try
            {
                searchRequest = JsonSerializer.Deserialize<VectorSearchRequest>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing request body");
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request payload.");
                return badResponse;
            }

            IEmbeddingService embeddingService;
            if (!string.IsNullOrWhiteSpace(searchRequest.Platform))
            {
                if (searchRequest.Platform.Equals("openai", StringComparison.OrdinalIgnoreCase))
                {
                    embeddingService = new OpenAIEmbeddingService(_httpClientFactory.CreateClient());
                }
                else if (searchRequest.Platform.Equals("voyage", StringComparison.OrdinalIgnoreCase))
                {
                    embeddingService = new VoyageEmbeddingService(_httpClientFactory.CreateClient());
                }
                else
                {
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await errorResponse.WriteStringAsync("Invalid platform specified.");
                    return errorResponse;
                }
            }
            else
            {
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Platform is required in the request.");
                return errorResponse;
            }

            if (searchRequest == null || string.IsNullOrWhiteSpace(searchRequest.Text))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Please provide valid text input.");
                return badResponse;
            }

            int limit = searchRequest.Limit > 0 ? searchRequest.Limit : 10; // default value

            try
            {
                // Obtain the embedding.
                var texts = new List<string> { searchRequest.Text };
                var embeddings = await embeddingService.GetEmbeddingsAsync(texts, searchRequest.EmbeddingModel, "document");
                if (embeddings == null || embeddings.Count == 0)
                {
                    var internalResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await internalResponse.WriteStringAsync("Failed to obtain embedding.");
                    return internalResponse;
                }
                var embeddingVector = embeddings[0].EmbeddingValues.Select(d => (float)d).ToArray();

                // Prepare and send the vector search request.
                using var client = _httpClientFactory.CreateClient();
                string searchUrl = $"https://serverlesslancedb.azurewebsites.net/api/{searchRequest.OrgId}/search";
                var searchPayload = new
                {
                    vector = embeddingVector,
                    limit = limit,
                    returnVector = searchRequest.ReturnVector
                };

                var response = await client.PostAsJsonAsync(searchUrl, searchPayload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = req.CreateResponse(response.StatusCode);
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    await errorResponse.WriteStringAsync($"Vector search failed: {errorDetail}");
                    return errorResponse;
                }
                string vectorSearchResponse = await response.Content.ReadAsStringAsync();
                List<VectorSearchResult> searchResult = JsonSerializer.Deserialize<List<VectorSearchResult>>(vectorSearchResponse);
                var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await okResponse.WriteAsJsonAsync(searchResult);
                return okResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during vector search process");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("An error occurred during vector search.");
                return errorResponse;
            }
        }
    }

    public class VectorSearchRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("orgId")]
        public string OrgId { get; set; }
        [JsonPropertyName("platform")]
        public string Platform { get; set; }
        [JsonPropertyName("embeddingModel")]
        public string EmbeddingModel { get; set; }
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
        [JsonPropertyName("returnVector")]
        public bool ReturnVector { get; set; } = false;

        // Serialize the object to JSON string
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        // Deserialize a JSON string to an object
        public static VectorSearchRequest FromJson(string json)
        {
            return JsonSerializer.Deserialize<VectorSearchRequest>(json);
        }
    }

}
