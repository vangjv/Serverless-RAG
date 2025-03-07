using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServerlessRAG.VectorEmbedding.Models;

namespace ServerlessRAG.VectorEmbedding.Services
{
    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAIEmbeddingService(HttpClient httpClient, string openAIAPIKey = null)
        {
            _httpClient = httpClient;
            _apiKey = openAIAPIKey ?? Environment.GetEnvironmentVariable("OpenAIAPIKey");
        }

        public async Task<List<Embedding>> GetEmbeddingsAsync(
            List<string> input,
            string model = "text-embedding-3-large",
            string inputType = null)
        {
            // Build the payload as per OpenAI embedding API requirements.
            var payload = new
            {
                model,
                input
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(payload, jsonOptions);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Set the authorization header.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // Deserialize the response stream.
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var embeddingResponse = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(
                responseStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return embeddingResponse?.Data ?? new List<Embedding>();
        }
    }
}
