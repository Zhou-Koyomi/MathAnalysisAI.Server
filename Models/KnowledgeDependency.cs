using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class KnowledgeDependency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FromKnowledgePointId { get; set; }

        [Required]
        public int ToKnowledgePointId { get; set; }

        [MaxLength(64)]
        public string DependencyType { get; set; } = "prerequisite";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public KnowledgePoint? FromKnowledgePoint { get; set; }
        public KnowledgePoint? ToKnowledgePoint { get; set; }
    }
}
