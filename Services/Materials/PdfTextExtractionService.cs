using UglyToad.PdfPig;

namespace MathAnalysisAI.Server.Services.Materials
{
    public class PdfTextExtractionService
    {
        public Task<PdfExtractionResult> ExtractAsync(string absolutePath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pages = new List<PdfPageText>();
            using var doc = PdfDocument.Open(absolutePath);
            foreach (var page in doc.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = page.Text ?? string.Empty;
                pages.Add(new PdfPageText
                {
                    PageNumber = page.Number,
                    Text = text
                });
            }

            var totalChars = pages.Sum(x => x.Text?.Length ?? 0);
            return Task.FromResult(new PdfExtractionResult
            {
                Pages = pages,
                TotalTextLength = totalChars
            });
        }
    }

    public class PdfExtractionResult
    {
        public List<PdfPageText> Pages { get; set; } = new();
        public int TotalTextLength { get; set; }
    }

    public class PdfPageText
    {
        public int PageNumber { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
