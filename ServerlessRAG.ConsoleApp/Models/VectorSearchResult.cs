using ServerlessRAG.Unstructured.Enums;
using System.Text.Json.Serialization;

namespace ServerlessRAG.ConsoleApp.Models
{
    public class VectorSearchResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("vector")]
        public float[] Vector { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("sourceElementIds")]
        public List<string> SourceElementIds { get; set; }
        [JsonPropertyName("pageNumbers")]
        public List<int> PageNumbers { get; set; }
        [JsonPropertyName("chunkType")]
        public string ChunkType { get; set; }
        [JsonPropertyName("strategy")]
        public ChunkingStrategy Strategy { get; set; }
        [JsonPropertyName("_distance")]
        public decimal Distance { get; set; }
    }
}
