using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class StudentSolution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProblemId { get; set; }

        public int? UserId { get; set; }

        [Required]
        public string SolutionText { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public Problem? Problem { get; set; }
        public AppUser? User { get; set; }
        public ICollection<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
    }
}
