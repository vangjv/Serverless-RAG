namespace ServerlessRAG.ConsoleApp
{
    public class UploadSampleDocs
    {
        public async Task RunAsync()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string sampleDocsPath = Path.Combine(baseDirectory, "SampleDocs");
            await DocumentUploader.UploadPdfDocumentsAsync(sampleDocsPath, Environment.GetEnvironmentVariable("DocumentProcessorEndpoint"), "testing");
        }
    }
}
