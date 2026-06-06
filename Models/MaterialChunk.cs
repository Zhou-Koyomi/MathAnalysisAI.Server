using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class MaterialChunk
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseMaterialId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int? ChapterId { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        [Required]
        [MaxLength(32)]
        public string ChunkType { get; set; } = "unknown";

        [MaxLength(256)]
        public string? SemanticTitle { get; set; }

        [MaxLength(256)]
        public string? SectionTitle { get; set; }

        [MaxLength(512)]
        public string? SectionPath { get; set; }

        public int? PageStart { get; set; }
        public int? PageEnd { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ContentPreview { get; set; } = string.Empty;

        public string? FormulaText { get; set; }
        public string? NormalizedFormulaText { get; set; }

        public int TokenCountEstimate { get; set; }

        public int? StartOffset { get; set; }
        public int? EndOffset { get; set; }

        [MaxLength(16)]
        public string? DifficultyLevel { get; set; }

        public bool IsVerified { get; set; }

        public int? VerifiedByUserId { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CourseMaterial? CourseMaterial { get; set; }
        public Course? Course { get; set; }
        public Chapter? Chapter { get; set; }
        public AppUser? VerifiedByUser { get; set; }
        public ICollection<MaterialChunkKnowledgePoint> KnowledgePointLinks { get; set; } = new List<MaterialChunkKnowledgePoint>();
    }
}
