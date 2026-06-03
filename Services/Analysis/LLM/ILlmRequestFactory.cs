using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.AnalysisContext;
using MathAnalysisAI.Server.DTOs.LLM;
using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Analysis.LLM
{
    public interface ILlmRequestFactory
    {
        Task<LLMChatRequestDto> BuildAsync(
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            Problem problem,
            StudentSolution? studentSolution,
            AnalysisContextDto? context,
            int analysisResultId,
            CancellationToken cancellationToken);
    }
}
