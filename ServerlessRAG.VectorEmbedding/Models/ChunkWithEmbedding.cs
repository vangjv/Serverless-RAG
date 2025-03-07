using ServerlessRAG.Unstructured.Enums;
using ServerlessRAG.Unstructured.Models;
using System.Text.Json.Serialization;

namespace ServerlessRAG.VectorEmbedding.Models
{
    public class ChunkWithEmbedding
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

        public static ChunkWithEmbedding FromChunk(Chunk chunk, string id, float[] vector)
        {
            return new ChunkWithEmbedding
            {
                Id = id,
                Text = chunk.Text,
                SourceElementIds = chunk.Metadata.SourceElementIds,
                PageNumbers = chunk.Metadata.PageNumbers,
                ChunkType = chunk.Metadata.ChunkType,
                Strategy = chunk.Metadata.Strategy,
                Vector = vector
            };
        }

    }
}
