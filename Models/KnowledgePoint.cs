using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class KnowledgePoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int? ChapterId { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? Code { get; set; }

        [MaxLength(1024)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Course? Course { get; set; }
        public Chapter? Chapter { get; set; }
        public ICollection<KnowledgeDependency> OutgoingDependencies { get; set; } = new List<KnowledgeDependency>();
        public ICollection<KnowledgeDependency> IncomingDependencies { get; set; } = new List<KnowledgeDependency>();
        public ICollection<MistakeRecord> MistakeRecords { get; set; } = new List<MistakeRecord>();
        public ICollection<UserKnowledgeState> UserKnowledgeStates { get; set; } = new List<UserKnowledgeState>();
    }
}
