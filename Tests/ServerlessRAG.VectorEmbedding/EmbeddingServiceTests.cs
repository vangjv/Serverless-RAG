using NUnit.Framework;
using System.Net;
using System.Text;
using ServerlessRAG.VectorEmbedding.Services;

namespace ServerlessRAG.VectorEmbedding.Tests
{
    [TestFixture]
    public class EmbeddingServiceTests
    {
        // A simple HttpMessageHandler that always returns a preset response.
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public TestHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Optionally, assert properties of 'request' here.
                return Task.FromResult(_response);
            }
        }

        [Test]
        public async Task GetEmbeddingsAsync_ReturnsValidEmbeddings()
        {
            Console.WriteLine("Before calling GetEmbeddingsAsync");
            try
            {
                // Arrange
                // Sample JSON response from Voyage API as per the documentation.
                var sampleJson = @"
            {
                ""object"": ""list"",
                ""data"": [
                    {
                        ""object"": ""embedding"",
                        ""embedding"": [0.123, 0.456],
                        ""index"": 0
                    }
                ]
            }";
               
                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
                };

                var handler = new TestHttpMessageHandler(responseMessage);
                var httpClient = new HttpClient(handler);
                var embeddingService = new VoyageEmbeddingService(httpClient, Environment.GetEnvironmentVariable("VoyageAPIKey"));

                var inputTexts = new List<string> { "This is a test." };
                var model = "voyage-3-lite";

                // Act
                var embeddings = await embeddingService.GetEmbeddingsAsync(inputTexts, model, "query");

                // Assert
                Assert.That(embeddings, Is.Not.Null);
                Assert.That(embeddings.Count, Is.EqualTo(1));

                var embedding = embeddings[0];
                Assert.That(embedding.Index, Is.EqualTo(0));
                Assert.That(embedding.Object, Is.EqualTo("embedding"));
                Assert.That(embedding.EmbeddingValues, Is.EqualTo(new List<double> { 0.123, 0.456 }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }
    }
}
