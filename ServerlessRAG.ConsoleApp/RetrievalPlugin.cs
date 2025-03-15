using Microsoft.SemanticKernel;
using ServerlessRAG.ConsoleApp.Models;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace ServerlessRAG.ConsoleApp
{
    public class RetrievalPlugin
    {
        [KernelFunction]
        [Description("Perform a vector similarity search to retrieve relevant documents for answering the users inquiry")]
        public async Task<string> GetRelevantDocuments([Description("The users query which will be embedding and compared to other embeddings in the vector database")] string query)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("VectorSearchEndpoint"));
            VectorSearchRequest searchRequest = new VectorSearchRequest
            {
                Text = query,
                OrgId = "hakunamatata",
                Limit = 5,
                Platform = "openai",
                EmbeddingModel = "text-embedding-3-large",
                ReturnVector = false
            };
            var content = new StringContent(searchRequest.ToJson(), null, "application/json");
            request.Content = content;

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                List<VectorSearchResult> results = JsonSerializer.Deserialize<List<VectorSearchResult>>(await response.Content.ReadAsStringAsync());
                string combinedText = BuildCombinedText(results);
                Console.WriteLine(combinedText);
                return combinedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public string BuildCombinedText(List<VectorSearchResult> results)
        {
            var sb = new StringBuilder();
            foreach (var result in results)
            {
                // Add filename header
                sb.AppendLine($"Filename: {result.FileName}");

                // Include page numbers if available
                if (result.PageNumbers != null && result.PageNumbers.Count > 0)
                {
                    sb.AppendLine($"Page Numbers: {string.Join(", ", result.PageNumbers)}");
                }
                // Add the text content
                sb.AppendLine("Text:");
                sb.AppendLine(result.Text);

                // Use a separator for clarity between entries
                sb.AppendLine(new string('-', 80));
            }
            return sb.ToString();
        }
    }
}
