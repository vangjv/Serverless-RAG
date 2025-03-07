using ServerlessRAG.VectorEmbedding.Models;

namespace ServerlessRAG.VectorEmbedding.Services
{
    public interface IEmbeddingService
    {
        /// <summary>
        /// Calls the VoyageAPI to retrieve text embeddings.
        /// </summary>
        /// <param name="input">A list of text strings to embed.</param>
        /// <param name="model">The model name (e.g. "voyage-large-2").</param>
        /// <param name="inputType">Optional input type ("query" or "document").</param>
        /// <returns>A list of embedding objects.</returns>
        Task<List<Embedding>> GetEmbeddingsAsync(
            List<string> input,
            string model,
            string inputType = null);
    }
}
