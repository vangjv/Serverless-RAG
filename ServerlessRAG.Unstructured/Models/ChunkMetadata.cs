using ServerlessRAG.Unstructured.Enums;

namespace ServerlessRAG.Unstructured.Models
{
    public class ChunkMetadata
    {
        public string FileName { get; set; }
        /// <summary>
        /// List of source element IDs that were combined into this chunk.
        /// </summary>
        public List<string> SourceElementIds { get; set; } = new List<string>();

        /// <summary>
        /// List of page numbers included in the chunk.
        /// </summary>
        public List<int> PageNumbers { get; set; } = new List<int>();

        /// <summary>
        /// A label for the type of chunk produced.
        /// </summary>
        public string ChunkType { get; set; }
        /// <summary>
        /// The chunking strategy used to produce this chunk.
        /// </summary>
        public ChunkingStrategy Strategy { get; set; }
    }
}
