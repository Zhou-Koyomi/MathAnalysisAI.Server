using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class UserKnowledgeState
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int KnowledgePointId { get; set; }

        public int MasteryLevel { get; set; }
        public int PracticeCount { get; set; }
        public int CorrectCount { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public AppUser? User { get; set; }
        public KnowledgePoint? KnowledgePoint { get; set; }
    }
}
