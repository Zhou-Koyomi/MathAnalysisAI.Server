using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.DTOs.LLM;
using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Analysis.Persistence
{
    public interface IAnalysisPersistenceService
    {
        Task<(Problem Problem, StudentSolution? StudentSolution)> CreateProblemAggregateAsync(
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            CancellationToken cancellationToken);

        Task<AnalysisResult> CreatePendingAnalysisResultAsync(
            Problem problem,
            StudentSolution? studentSolution,
            string analysisMode,
            Course course,
            Chapter? chapter,
            CancellationToken cancellationToken);

        Task<AnalysisResult> SaveLlmFailedAsync(
            AnalysisResult pending,
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            LLMChatResponseDto llmResponse,
            CancellationToken cancellationToken);

        Task<AnalysisResult> SaveParseFailedAsync(
            AnalysisResult pending,
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            LLMChatResponseDto llmResponse,
            string parseError,
            CancellationToken cancellationToken);

        Task<AnalysisResult> SaveSchemaInvalidAsync(
            AnalysisResult pending,
            AnalysisRequestDto request,
            Course course,
            Chapter? chapter,
            LLMChatResponseDto llmResponse,
            AnalysisResponseDto parsed,
            string validationError,
            CancellationToken cancellationToken);

        Task<AnalysisResult> SaveSuccessAsync(
            AnalysisResult pending,
            AnalysisResponseDto parsed,
            LLMChatResponseDto llmResponse,
            CancellationToken cancellationToken);
    }
}
