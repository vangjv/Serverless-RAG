using System.Text.Json.Serialization;

namespace ServerlessRAG.VectorEmbedding.Models
{
    public class VectorSearchResult:ChunkWithEmbedding
    {
        [JsonPropertyName("_distance")]
        public decimal Distance { get; set; }
    }
}
