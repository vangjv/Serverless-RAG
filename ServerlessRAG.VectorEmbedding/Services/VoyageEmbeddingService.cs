using ServerlessRAG.VectorEmbedding.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServerlessRAG.VectorEmbedding.Services
{
    public class VoyageEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public VoyageEmbeddingService(HttpClient httpClient, string voyageAPIKey = null)
        {
            _httpClient = httpClient;
            _apiKey = voyageAPIKey ?? Environment.GetEnvironmentVariable("VoyageAPIKey");
        }

        public async Task<List<Embedding>> GetEmbeddingsAsync(List<string> input, string model = "voyage-3-large", string inputType = "document")
        {
            if (Environment.GetEnvironmentVariable("VoyageEmbeddingModel") != null)
            {
                model = Environment.GetEnvironmentVariable("VoyageEmbeddingModel");
            }   
            // Build the payload as per the documentation.
            var payload = new
            {
                input,
                model,
                input_type = inputType,
                // Optional: You can add other parameters like truncation, output_dimension, etc.
                truncation = true
            };

            var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var json = JsonSerializer.Serialize(payload, jsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.voyageai.com/v1/embeddings")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Set the authorization header.
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // Deserialize the response stream.
            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var embeddingResponse = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(responseStream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return embeddingResponse?.Data ?? new List<Embedding>();
        }
    }
}
