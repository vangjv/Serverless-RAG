using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerlessRAG.Unstructured.Models;

namespace ServerlessRAG.Unstructured
{
    public interface IIngestionService
    {
        Task<List<Element>> IngestDocumentAsync(System.IO.Stream fileStream, string fileName, string strategy);
    }

    public class IngestionService : IIngestionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IngestionService> _logger;
        private readonly IConfiguration _configuration;

        public IngestionService(IHttpClientFactory httpClientFactory, ILogger<IngestionService> logger, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<List<Element>> IngestDocumentAsync(System.IO.Stream fileStream, string fileName, string ingestionStrategy)
        {
            // Retrieve configuration values
            var apiUrl = _configuration["Unstructured:ApiUrl"];
            var apiKey = _configuration["Unstructured:ApiKey"];
            var strategy = ingestionStrategy ?? "fast";

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("unstructured-api-key", apiKey);

            // Create multipart content
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);

            // Set the correct media type based on the file extension
            var mediaType = GetMediaType(fileName);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

            // Note: Use the expected field name "files" and pass the file name
            content.Add(streamContent, "files", fileName);
            content.Add(new StringContent(strategy), "strategy");

            request.Content = content;

            try
            {
                using var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Element>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling unstructured API.");
                throw;
            }
        }

        private string GetMediaType(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".csv" => "text/csv",
                ".html" => "text/html",
                ".md" => "text/markdown",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream",
            };
        }
    }
}
