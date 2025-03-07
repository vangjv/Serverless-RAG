using ServerlessRAG.Unstructured;
using ServerlessRAG.Unstructured.Models;
using System.Text.Json;

namespace ServerlessRAG.ConsoleApp.Chunking
{
    public class ChunkTestService
    {
        private List<Element> Elements { get; set; }

        public async Task LoadElementsFromFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            var jsonString = await File.ReadAllTextAsync(filePath);
            Elements = JsonSerializer.Deserialize<List<Element>>(jsonString);
        }

        public async Task<List<Element>> ReadElementsFromFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            try
            {
                var jsonString = await File.ReadAllTextAsync(filePath);
                var element = JsonSerializer.Deserialize<List<Element>>(jsonString);
                return element;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading the file: {ex.Message}");
                throw;
            }
        }

        public async Task GenerateTestChunksAsync()
        {
            //chunksByTitle
            var chunksByTitle = Chunker.ChunkByTitle(Elements);
            var chunksJson = JsonSerializer.Serialize(chunksByTitle);
            await File.WriteAllTextAsync("./Chunking/chunksByTitle.json", chunksJson);
            Console.WriteLine($"Number of chunks chunksByTitle: {chunksByTitle.Count}");

            //chunksByPage
            var chunksByPage = Chunker.PageLevelChunking(Elements);
            var chunksByPageJson = JsonSerializer.Serialize(chunksByPage);
            await File.WriteAllTextAsync("./Chunking/chunksByPage.json", chunksByPageJson);
            Console.WriteLine($"Number of chunks chunksByPage: {chunksByPage.Count}");

            //chunksByParentChild
            var chunksByGroup = Chunker.ParentChildGrouping(Elements);
            var chunksByGroupJson = JsonSerializer.Serialize(chunksByGroup);
            await File.WriteAllTextAsync("./Chunking/chunksByGroup.json", chunksByGroupJson);
            Console.WriteLine($"Number of chunks chunksByGroup: {chunksByGroup.Count}");

            var singleChunk = Chunker.CombineAllElementsIntoSingleChunk(Elements);

            //chunksBySlidingWindow
            var chunksSlidingWindow = Chunker.SlidingWindowChunking(singleChunk, 1000, 200);
            var chunksSlidingWindowJson = JsonSerializer.Serialize(chunksSlidingWindow);
            await File.WriteAllTextAsync("./Chunking/chunksSlidingWindow.json", chunksSlidingWindowJson);
            Console.WriteLine($"Number of chunks chunksSlidingWindow: {chunksSlidingWindow.Count}");

            //chunksByFixedSize
            var chunksFixedSize = Chunker.FixedSizeChunking(singleChunk, 1000);
            var chunksFixedSizeJson = JsonSerializer.Serialize(chunksFixedSize);
            await File.WriteAllTextAsync("./Chunking/chunksFixedSize.json", chunksFixedSizeJson);
            Console.WriteLine($"Number of chunks chunksFixedSize: {chunksFixedSize.Count}");

            //chunksByRecursiveCharacter
            var chunksRecursiveCharacter = Chunker.RecursiveCharacterTextSplitting(singleChunk, 1000);
            var chunksRecursiveCharacterJson = JsonSerializer.Serialize(chunksRecursiveCharacter);
            await File.WriteAllTextAsync("./Chunking/chunksRecursiveCharacter.json", chunksRecursiveCharacterJson);
            Console.WriteLine($"Number of chunks chunksRecursiveCharacter: {chunksRecursiveCharacter.Count}");
        }
    }
}
