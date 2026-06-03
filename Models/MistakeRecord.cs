using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class MistakeRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnalysisResultId { get; set; }

        public int? KnowledgePointId { get; set; }

        [Required]
        [MaxLength(128)]
        public string MistakeTag { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Description { get; set; }

        public int Severity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AnalysisResult? AnalysisResult { get; set; }
        public KnowledgePoint? KnowledgePoint { get; set; }
    }
}
