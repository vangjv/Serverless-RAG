using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServerlessRAG.Unstructured;
using ServerlessRAG.Unstructured.Models;
using ServerlessRAG.DocumentProcessing.Models;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class ChunkElementsActivity
    {
        [Function("ChunkElementsActivity")]
        public static async Task<List<Chunk>> Run(
            [ActivityTrigger] ChunkElementsActivityInput input,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ChunkElementsActivity");
            logger.LogInformation($"Starting chunking for job '{input.DocumentProcessorJobId}' using strategy '{input.Options?.Strategy}'.");

            // Determine the chunking method based on the strategy.
            List<Chunk> chunks;
            string strategy = input.Options?.Strategy?.ToLowerInvariant();
            switch (strategy)
            {
               
                case "pagelevel":
                    chunks = Chunker.PageLevelChunking(input.Elements);
                    break;
                case "semanticstructural":
                    chunks = Chunker.SemanticStructuralGrouping(input.Elements);
                    break;
                case "contentspecific":
                    chunks = Chunker.ContentSpecificChunking(input.Elements);
                    break;
                case "elementbased":
                    chunks = Chunker.ElementBasedChunking(input.Elements);
                    break;
                case "parentchild":        
                default:
                    // Default to parentChild chunking.
                    chunks = Chunker.ParentChildGrouping(input.Elements);
                    break;
            }

            // Instead of uploading here, simply return the generated chunks.
            logger.LogInformation($"Generated {chunks.Count} chunks.");
            return chunks;
        }
    }

    // Update the input model so that the chunking options is an object.
    public class ChunkElementsActivityInput
    {
        public string OrgId { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public List<Element> Elements { get; set; }
        public ChunkingOptions Options { get; set; }
    }
}
