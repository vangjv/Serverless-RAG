using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0070
namespace ServerlessRAG.ConsoleApp
{
    public class RAGStreamingChat
    {
        public async Task RunAsync()
        {
            var builder = Kernel.CreateBuilder()
                   .AddOpenAIChatCompletion("gpt-4o", Environment.GetEnvironmentVariable("OpenAIKey"));
            Kernel kernel = builder.Build();
            kernel.Plugins.AddFromType<RetrievalPlugin>();
            var settings = new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            ChatHistory chat = new();
            StringBuilder responseBuilder = new StringBuilder();
            var ragPrompt = @"
<ROLE>
""You are a research assistant tasked with answering the user’s question. Your answer must be fully grounded in the documents retrieved from the tool, and you must not include any information that is not supported by these documents. Please follow these steps:
</ROLE>
<INSTRUCTIONS>
1. **Document Retrieval:**  
   - Use the retrieval tool/function GetRelevantDocuments to search for and retrieve documents relevant to the user’s question. Use the users question as the query.
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
            while (true)
            {
                Console.WriteLine("Q: ");
                chat.AddUserMessage(Console.ReadLine());
                responseBuilder = new StringBuilder();
                Console.WriteLine("Assistant: ");
                await foreach (var response in chatService.GetStreamingChatMessageContentsAsync(chat, settings, kernel))
                {
                    responseBuilder.Append(response);
                    Console.Write(response);
                }
                Console.WriteLine();
                chat.AddAssistantMessage(responseBuilder.ToString());
            }
        }

    }
}
