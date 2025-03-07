using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.Unstructured.Models;
using ServerlessRAG.VectorEmbedding.Models;
using ServerlessRAG.VectorEmbedding.Services;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class EmbedChunksActivity
    {
        [Function("EmbedChunksActivity")]
        public static async Task<List<ChunkWithEmbedding>> Run(
            [ActivityTrigger] EmbedChunksActivityInput input,
            FunctionContext context)
        {
            var logger = context.GetLogger("EmbedChunksActivity");

            if (input.Chunks == null || !input.Chunks.Any())
            {
                logger.LogWarning("No chunks provided for embedding.");
                return new List<ChunkWithEmbedding>();
            }

            // Create a new HttpClient instance.
            using var httpClient = new HttpClient();
            IEmbeddingService embeddingService;
            switch (input.Platform?.Trim().ToLowerInvariant())
            {
                case "openai":
                    embeddingService = new OpenAIEmbeddingService(httpClient);
                    break;
                case "voyage":
                    embeddingService = new VoyageEmbeddingService(httpClient);
                    break;
                default:
                    throw new Exception($"Unsupported embedding platform: '{input.Platform}'.");
            }

            // Extract the text from each chunk.
            var texts = input.Chunks.Select(chunk => chunk.Text).ToList();

            // Call the embedding service with the list of texts.
            var embeddings = await embeddingService.GetEmbeddingsAsync(texts, input.Model, input.InputType);

            // Ensure the returned embeddings count matches the input chunks count.
            if (embeddings.Count != input.Chunks.Count)
            {
                logger.LogError("Mismatch between number of chunks and returned embeddings.");
                throw new Exception("Embedding count mismatch.");
            }

            // Create a new list of ChunkWithEmbedding.
            var chunksWithEmbedding = new List<ChunkWithEmbedding>();
            for (int i = 0; i < input.Chunks.Count; i++)
            {
                var chunk = input.Chunks[i];
                var embedding = embeddings[i];
                // Convert the embedding vector from List<double> to float[].
                float[] vector = embedding.EmbeddingValues.Select(d => (float)d).ToArray();
                // Generate a new ID and create the ChunkWithEmbedding.
                string id = Guid.NewGuid().ToString();
                chunksWithEmbedding.Add(ChunkWithEmbedding.FromChunk(chunk, id, vector));
            }
            return chunksWithEmbedding;
        }
    }

    public class EmbedChunksActivityInput
    {
        public List<Chunk> Chunks { get; set; }
        public string Platform { get; set; }
        public string Model { get; set; }
        public string InputType { get; set; } = "document"; // e.g. "document" or "query"
    }
}
