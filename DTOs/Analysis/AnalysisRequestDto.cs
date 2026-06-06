using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.DTOs.Analysis
{
    public class AnalysisRequestDto
    {
        [Required]
        public int CourseId { get; set; }

        public int? ChapterId { get; set; }

        [Required]
        public string ProblemText { get; set; } = string.Empty;

        public string? StudentSolutionText { get; set; }

        [Required]
        public string AnalysisMode { get; set; } = string.Empty;

        public int? UserId { get; set; }
    }
}
