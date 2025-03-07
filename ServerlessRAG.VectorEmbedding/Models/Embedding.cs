using System.Text.Json.Serialization;

namespace ServerlessRAG.VectorEmbedding.Models
{
    // Data model representing each embedding object.
    public class Embedding
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }

        // This property maps to the "embedding" field in the API response.
        [JsonPropertyName("embedding")]
        public List<double> EmbeddingValues { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
