using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class MaterialChunkKnowledgePoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaterialChunkId { get; set; }

        [Required]
        public int KnowledgePointId { get; set; }

        [Required]
        [MaxLength(32)]
        public string RelationType { get; set; } = "related";

        public bool IsPrimary { get; set; }

        public decimal Confidence { get; set; } = 1.0000m;

        [Required]
        [MaxLength(16)]
        public string Source { get; set; } = "rule";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public MaterialChunk? MaterialChunk { get; set; }
        public KnowledgePoint? KnowledgePoint { get; set; }
    }
}
