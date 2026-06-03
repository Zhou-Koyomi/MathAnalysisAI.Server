namespace MathAnalysisAI.Server.DTOs.Analysis
{
    public class StudentSolutionReviewDto
    {
        public bool? IsCorrect { get; set; }
        public string? MainIssue { get; set; }
        public List<string> LogicGaps { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}
