namespace MathAnalysisAI.Server.DTOs.Knowledge
{
    public class KnowledgeRetrievalRequest
    {
        public int CourseId { get; set; }
        public int? ChapterId { get; set; }
        public string ProblemText { get; set; } = string.Empty;
        public string? StudentSolutionText { get; set; }
        public IReadOnlyList<string>? NormalizedKnowledgePointCodes { get; set; }
        public int TopK { get; set; } = 3;
    }
}
