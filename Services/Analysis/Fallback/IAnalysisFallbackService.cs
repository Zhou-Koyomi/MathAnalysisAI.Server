using MathAnalysisAI.Server.DTOs.Analysis;

namespace MathAnalysisAI.Server.Services.Analysis.Fallback
{
    public interface IAnalysisFallbackService
    {
        void ApplyFallbacks(
            AnalysisResponseDto parsed,
            string analysisMode,
            string problemText,
            string? studentSolutionText,
            string? rawLlmContent,
            string? chapterName);
    }
}
