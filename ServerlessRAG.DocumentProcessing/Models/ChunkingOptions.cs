using System.Text.Json.Serialization;

namespace ServerlessRAG.DocumentProcessing.Models
{
    public class ChunkingOptions
    {
        // The name of the strategy. This should match the keys used in the Chunker class.
        [JsonPropertyName("strategy")]
        public string Strategy { get; set; }
        
        // Optional: add additional parameters for specific strategies.
        // For example, maximum pages for title-based chunking.
        public int? MaxPagesWithoutTitle { get; set; }
        
        // You can add more properties as needed for parameterizing your chunk methods.
    }
}
