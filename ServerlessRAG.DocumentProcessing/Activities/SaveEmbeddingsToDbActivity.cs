using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.VectorEmbedding.Models;

namespace ServerlessRAG.DocumentProcessing.Activities
{

    public static class SaveEmbeddingsToDbActivity
    {
        [Function("SaveEmbeddingsToDbActivity")]
        public static async Task<string> Run(
            [ActivityTrigger] SaveEmbeddingsToDbInput input,
            FunctionContext context)
        {
            var logger = context.GetLogger("SaveEmbeddingsToDbActivity");

            if (input.ChunksWithEmbedding == null || !input.ChunksWithEmbedding.Any())
            {
                logger.LogWarning("No chunk embeddings provided for bulk upload.");
                return "No chunks to upload.";
            }

            using var httpClient = new HttpClient();
            string url = $"https://serverlesslancedb.azurewebsites.net/api/{input.OrgId}/bulk_items";

            var response = await httpClient.PostAsJsonAsync(url, input.ChunksWithEmbedding);
            if (response.IsSuccessStatusCode)
            {
                return $"Uploaded {input.ChunksWithEmbedding.Count} bulk chunk embeddings.";
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError($"Bulk upload failed. Status: {response.StatusCode}. Details: {errorContent}");
                throw new Exception($"Bulk upload of chunk embeddings failed with status code: {response.StatusCode}");
            }
        }
    }

    public class SaveEmbeddingsToDbInput
    {
        public string OrgId { get; set; }
        public List<ChunkWithEmbedding> ChunksWithEmbedding { get; set; }
    }

}
