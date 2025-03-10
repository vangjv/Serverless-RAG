namespace ServerlessRAG.ConsoleApp
{
    public class DocumentUploader
    {
        public static async Task UploadPdfDocumentsAsync(string folderPath, string documentProcessorUrl, string orgId, string strategy = "fast")
        {
            // Reuse the same HttpClient instance for all requests
            using (var client = new HttpClient())
            {
                // Find all PDF files in the provided folder
                var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");

                foreach (var file in pdfFiles)
                {
                    // Create a new HttpRequestMessage for each file
                    var request = new HttpRequestMessage(HttpMethod.Post, documentProcessorUrl);

                    // Create the multipart/form-data content
                    var content = new MultipartFormDataContent();

                    // Add the file content
                    content.Add(new StreamContent(File.OpenRead(file)), "file", Path.GetFileName(file));

                    // Add the required form fields
                    content.Add(new StringContent(orgId), "orgId");
                    content.Add(new StringContent("{\"strategy\":\"pagelevel\"}"), "chunkingOptions");
                    content.Add(new StringContent(strategy), "ingestionStrategy");
                    content.Add(new StringContent("openai"), "embeddingPlatform");
                    content.Add(new StringContent("text-embedding-3-large"), "embeddingModel");

                    // Attach our content to the request
                    request.Content = content;

                    // Send the request and wait for the response
                    var response = await client.SendAsync(request);

                    // Ensure it succeeded or throw an exception
                    response.EnsureSuccessStatusCode();

                    // Optionally read and print response content to console
                    string result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Uploaded {Path.GetFileName(file)} - Response: {result}");
                }
            }
        }
    }

}
