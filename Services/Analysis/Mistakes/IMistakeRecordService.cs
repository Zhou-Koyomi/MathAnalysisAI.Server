namespace MathAnalysisAI.Server.Services.Analysis.Mistakes
{
    public interface IMistakeRecordService
    {
        Task<List<int>> SaveMistakeRecordsAsync(
            int analysisResultId,
            int courseId,
            IReadOnlyList<string> normalizedKnowledgePointCodes,
            IReadOnlyList<string> mistakeTags,
            CancellationToken cancellationToken);
    }
}
