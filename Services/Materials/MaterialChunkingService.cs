using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Materials
{
    public class MaterialChunkingService
    {
        private const int TargetChunkSize = 1000;
        private const int OverlapSize = 120;
        private const int MinChunkLength = 200;

        public List<MaterialChunk> BuildChunks(
            int courseMaterialId,
            int courseId,
            int? chapterId,
            List<PdfPageText> pages)
        {
            var ordered = pages
                .OrderBy(x => x.PageNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .ToList();

            if (ordered.Count == 0)
            {
                return new List<MaterialChunk>();
            }

            var joined = string.Join("\n\n", ordered.Select(x => $"[Page {x.PageNumber}]\n{x.Text}"));
            var normalized = joined.Trim();
            if (normalized.Length == 0)
            {
                return new List<MaterialChunk>();
            }

            var chunks = new List<MaterialChunk>();
            var index = 0;
            var chunkIndex = 1;

            while (index < normalized.Length)
            {
                var remaining = normalized.Length - index;
                var length = Math.Min(TargetChunkSize, remaining);
                if (length <= 0)
                {
                    break;
                }

                var content = normalized.Substring(index, length).Trim();
                if (content.Length >= MinChunkLength || (chunks.Count == 0 && content.Length > 0))
                {
                    var preview = content.Length > 500 ? content[..500] : content;
                    var (pageStart, pageEnd) = EstimatePageRange(ordered, content);

                    chunks.Add(new MaterialChunk
                    {
                        CourseMaterialId = courseMaterialId,
                        CourseId = courseId,
                        ChapterId = chapterId,
                        ChunkIndex = chunkIndex++,
                        ChunkType = "unknown",
                        SemanticTitle = null,
                        SectionTitle = null,
                        SectionPath = null,
                        PageStart = pageStart,
                        PageEnd = pageEnd,
                        Content = content,
                        ContentPreview = preview,
                        FormulaText = null,
                        NormalizedFormulaText = null,
                        TokenCountEstimate = Math.Max(1, content.Length / 2),
                        StartOffset = index,
                        EndOffset = index + length,
                        DifficultyLevel = null,
                        IsVerified = false,
                        VerifiedByUserId = null,
                        VerifiedAt = null,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                if (index + length >= normalized.Length)
                {
                    break;
                }

                index += Math.Max(1, length - OverlapSize);
            }

            return chunks;
        }

        private static (int? PageStart, int? PageEnd) EstimatePageRange(List<PdfPageText> pages, string content)
        {
            // Simple heuristic for phase-1: infer by page markers in extracted text.
            var pageMatches = new List<int>();
            foreach (var p in pages)
            {
                if (content.Contains($"[Page {p.PageNumber}]", StringComparison.Ordinal))
                {
                    pageMatches.Add(p.PageNumber);
                }
            }

            if (pageMatches.Count == 0)
            {
                return (null, null);
            }

            return (pageMatches.Min(), pageMatches.Max());
        }
    }
}
