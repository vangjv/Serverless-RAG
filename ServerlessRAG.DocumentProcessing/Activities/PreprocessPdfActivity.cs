using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ServerlessRAG.DocumentProcessing.Activities
{
    public static class PreprocessPdfActivity
    {
        [Function("PreprocessPdfActivity")]
        public static async Task<List<PreprocessPdfSection>> Run(
            [ActivityTrigger] PreprocessPdfActivityInput input,
            FunctionContext context)
        {
            var logger = context.GetLogger("PreprocessPdfActivity");
            logger.LogInformation($"Starting PDF preprocessing for file '{input.FileName}' with {input.PagesPerSection} pages per section.");

            List<PreprocessPdfSection> sections = new List<PreprocessPdfSection>();

            using (var inputStream = new MemoryStream(input.FileBytes))
            {
                // Open the PDF document in Import mode.
                PdfDocument document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);
                int totalPages = document.PageCount;
                logger.LogInformation($"PDF document has {totalPages} pages.");

                int pagesPerSection = input.PagesPerSection;
                int currentPage = 1;
                // Process pages in groups defined by pagesPerSection.
                while (currentPage <= totalPages)
                {
                    PdfDocument newDoc = new PdfDocument();
                    for (int i = 0; i < pagesPerSection && (currentPage + i) <= totalPages; i++)
                    {
                        // PdfSharp indexes pages from 0.
                        PdfPage page = document.Pages[currentPage - 1 + i];
                        newDoc.AddPage(page);
                    }

                    using (var sectionStream = new MemoryStream())
                    {
                        newDoc.Save(sectionStream, false);
                        sections.Add(new PreprocessPdfSection
                        {
                            StartPage = currentPage,
                            SectionBytes = sectionStream.ToArray()
                        });
                    }

                    currentPage += pagesPerSection;
                }
            }

            logger.LogInformation($"Completed PDF preprocessing into {sections.Count} section(s).");
            return await Task.FromResult(sections);
        }
    }

    // Input definition for PreprocessPdfActivity.
    public class PreprocessPdfActivityInput
    {
        public string OrgId { get; set; }
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public string DocumentProcessorJobId { get; set; }
        public int PagesPerSection { get; set; }
    }

    // Return type for a single PDF section.
    public class PreprocessPdfSection
    {
        public byte[] SectionBytes { get; set; }
        public int StartPage { get; set; }
    }
}
