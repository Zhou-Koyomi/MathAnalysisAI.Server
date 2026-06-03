using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Analysis.Stats
{
    public interface IUserStatsUpdateService
    {
        bool? ResolveCorrectness(AnalysisResult? analysisResult);

        Task UpdateAfterAnalysisAsync(
            int? requestUserId,
            int? studentSolutionUserId,
            int courseId,
            IReadOnlyList<string> normalizedKnowledgePointCodes,
            IReadOnlyList<int> mistakeKnowledgePointIds,
            AnalysisResult analysisResult,
            CancellationToken cancellationToken);

        Task UpdateCourseStatsAsync(
            int? userId,
            int courseId,
            bool? isCorrect,
            CancellationToken cancellationToken);
    }
}
