using System.Text.Json.Serialization;

namespace ServerlessRAG.VectorEmbedding.Models
{
    // Data model representing the overall API response.
    public class EmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("data")]
        public List<Embedding> Data { get; set; }
    }

}
