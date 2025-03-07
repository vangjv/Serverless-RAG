namespace ServerlessRAG.Unstructured.Models
{
    public class Chunk
    {
        public string Text { get; set; }
        public ChunkMetadata Metadata { get; set; }
    }
}
