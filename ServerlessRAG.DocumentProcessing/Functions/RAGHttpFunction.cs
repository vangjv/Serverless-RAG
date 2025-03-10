using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using ServerlessRAG.Unstructured.Enums;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020

namespace ServerlessRAG.DocumentProcessing.Functions
{
    public class RAGHttpFunction
    {
        private readonly ILogger<RAGHttpFunction> _logger;

        public RAGHttpFunction(ILogger<RAGHttpFunction> logger)
        {
            _logger = logger;
        }

        [Function("RAGHttpFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rag")]
            HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var logger = context.GetLogger("RAGHttpFunction");
            var requestBody = await req.ReadAsStringAsync();
            var requestModel = JsonSerializer.Deserialize<RequestModel>(requestBody);

            if (string.IsNullOrEmpty(requestModel.UserMessage))
            {
                var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid input.");
                return badRequestResponse;
            }

            var builder = Kernel.CreateBuilder()
                   .AddOpenAIChatCompletion("gpt-4o", Environment.GetEnvironmentVariable("OpenAIAPIKey"));

            Kernel kernel = builder.Build();
            kernel.Plugins.AddFromObject(new RetrievalPlugin(requestModel.OrgId));
            //kernel.Plugins.AddFromType<RetrievalPlugin>();
            var settings = new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            ChatHistory chat = new();
            StringBuilder responseBuilder = new StringBuilder();
            var ragPrompt = @"
<ROLE>
""You are a research assistant tasked with answering the user�s question. Your answer must be fully grounded in the documents retrieved from the tool, and you must not include any information that is not supported by these documents. Please follow these steps:
</ROLE>
<INSTRUCTIONS>
1. **Document Retrieval:**  
   - Use the retrieval tool/function GetRelevantDocuments to search for and retrieve documents relevant to the user�s question. Use the users question as the query.
   - Make sure the documents cover all necessary aspects of the topic.

2. **Document Analysis:**  
   - Read through the retrieved documents carefully.
   - Extract key facts, data, and context that directly answer the user's question.

3. **Answer Composition:**  
   - Write a comprehensive and clear answer based strictly on the information from the retrieved documents.
   - Cite the sources explicitly where each piece of information is used, using the provided citation formatting.
   - Do not introduce any external or assumed information; only use what is present in the documents.
   - If there are gaps or ambiguities in the documents, note these in your answer instead of guessing.

4. **Verification:**  
   - Before finalizing your answer, double-check that every claim is supported by one of the retrieved sources.
   - Ensure that the answer is free from hallucinations and any unsupported information.
   - Do not hallucinate.
</INSTRUCTIONS>
";
            chat.AddSystemMessage(ragPrompt);
            chat.AddUserMessage(requestModel.UserMessage);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/event-stream");
            string chatResponse = "";
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chat, settings, kernel))
            {
                var data = $"data: {chunk}\n\n";
                chatResponse += chunk;
                await response.Body.WriteAsync(Encoding.UTF8.GetBytes(chunk.ToString()));
                await response.Body.FlushAsync();
            }
            Console.WriteLine(chatResponse);
            return response;
        }

        public class RequestModel
        {
            [JsonPropertyName("userMessage")]
            public string UserMessage { get; set; }
            [JsonPropertyName("orgId")]
            public string OrgId { get; set; }
        }

        public class RetrievalPlugin
        {
            private string OrgId { get; set; }
            private string EmbeddingModel { get; set; }
            private string Platform { get; set; }
            private int? Limit { get; set; }
            public RetrievalPlugin(string orgId, string? platform = "openai", string? embeddingModel = "text-embedding-3-large", int? limit = 5)
            {
                OrgId = orgId;
                EmbeddingModel = embeddingModel;
                Platform = platform;
                Limit = limit;
            }
            [KernelFunction]
            [Description("Perform a vector similarity search to retrieve relevant documents for answering the users inquiry")]
            public async Task<string> GetRelevantDocuments([Description("The users query which will be embedding and compared to other embeddings in the vector database")] string query)
            {
                var client = new HttpClient();
                var vectorSearchBaseUrl = Environment.GetEnvironmentVariable("VectorSearchBaseUrl");
                var request = new HttpRequestMessage(HttpMethod.Post, $"{vectorSearchBaseUrl}/api/vectorsearch");
                VectorSearchRequest searchRequest = new VectorSearchRequest
                {
                    Text = query,
                    OrgId = OrgId,
                    Limit = Limit ?? 5,
                    Platform = Platform,
                    EmbeddingModel = EmbeddingModel,
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
}
