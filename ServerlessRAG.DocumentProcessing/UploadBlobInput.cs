using ServerlessRAG.DocumentProcessing.Models;

public class UploadBlobInput
{
    public string ContainerName { get; set; }
    public string OrgId { get; set; }      // New: captures the organization id
    public string FileName { get; set; }
    public byte[] FileBytes { get; set; }
    public string DocumentProcessorJobId { get; set; }
    public string IngestionStrategy { get; set; } = "fast";
    public ChunkingOptions ChunkingOptions { get; set; }
    public string EmbeddingModel { get; set; }
    public string EmbeddingPlatform { get; set; }
}
