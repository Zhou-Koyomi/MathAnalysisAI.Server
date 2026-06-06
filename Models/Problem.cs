using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class Problem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int? ChapterId { get; set; }

        [MaxLength(256)]
        public string? Title { get; set; }

        [Required]
        public string ContentMarkdown { get; set; } = string.Empty;

        public string? ContentLatex { get; set; }

        [Required]
        [MaxLength(64)]
        public string SourceType { get; set; } = "text";

        [MaxLength(1024)]
        public string? SourceFilePath { get; set; }

        [Required]
        [MaxLength(64)]
        public string ProblemType { get; set; } = "mixed";

        [MaxLength(32)]
        public string? Difficulty { get; set; }

        public int? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Course? Course { get; set; }
        public Chapter? Chapter { get; set; }
        public AppUser? CreatedByUser { get; set; }
        public ICollection<StudentSolution> StudentSolutions { get; set; } = new List<StudentSolution>();
        public ICollection<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
    }
}
