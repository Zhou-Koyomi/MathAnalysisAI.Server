using MathAnalysisAI.Server.DTOs.Visualization;

namespace MathAnalysisAI.Server.DTOs.Analysis
{
    public class AnalysisResponseDto
    {
        public int? AnalysisResultId { get; set; }
        public int? ProblemId { get; set; }
        public int? StudentSolutionId { get; set; }

        public string Course { get; set; } = string.Empty;
        public string? Chapter { get; set; }
        public string ProblemType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;

        public List<string> KnowledgePoints { get; set; } = new();
        public string SolutionOverview { get; set; } = string.Empty;
        public List<StandardSolutionStepDto> StandardSolution { get; set; } = new();
        public StudentSolutionReviewDto StudentSolutionReview { get; set; } = new();
        public List<string> MistakeTags { get; set; } = new();
        public List<string> ReviewSuggestions { get; set; } = new();
        public VisualizationDto Visualization { get; set; } = new();
    }
}
