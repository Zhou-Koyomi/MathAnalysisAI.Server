using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class AnalysisResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProblemId { get; set; }

        public int? StudentSolutionId { get; set; }

        [Required]
        [MaxLength(64)]
        public string AnalysisMode { get; set; } = "review_solution";

        [MaxLength(128)]
        public string? CourseName { get; set; }

        [MaxLength(128)]
        public string? ChapterName { get; set; }

        [MaxLength(64)]
        public string? ProblemType { get; set; }

        [MaxLength(32)]
        public string? Difficulty { get; set; }

        public string? KnowledgePointsJson { get; set; }
        public string? StandardSolution { get; set; }
        public string? StudentSolutionReview { get; set; }
        public string? MistakeTagsJson { get; set; }
        public string? ReviewSuggestionsJson { get; set; }
        public string? RawResponseJson { get; set; }

        public bool? AiJudgedCorrect { get; set; }
        public bool? FinalCorrect { get; set; }

        [Required]
        [MaxLength(32)]
        public string FinalCorrectSource { get; set; } = CorrectnessSource.Ai;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Problem? Problem { get; set; }
        public StudentSolution? StudentSolution { get; set; }
        public ICollection<AnalysisVisualization> Visualizations { get; set; } = new List<AnalysisVisualization>();
        public ICollection<MistakeRecord> MistakeRecords { get; set; } = new List<MistakeRecord>();
    }
}
