using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? Code { get; set; }

        [MaxLength(512)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Subject? Subject { get; set; }
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public ICollection<Problem> Problems { get; set; } = new List<Problem>();
        public ICollection<PromptProfile> PromptProfiles { get; set; } = new List<PromptProfile>();
        public ICollection<UserCourseStats> UserCourseStats { get; set; } = new List<UserCourseStats>();
    }
}
