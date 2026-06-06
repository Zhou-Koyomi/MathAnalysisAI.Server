using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class UserCourseStats
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int AttemptCount { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public decimal AccuracyRate { get; set; }
        public decimal RankingScore { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public AppUser? User { get; set; }
        public Course? Course { get; set; }
    }
}
