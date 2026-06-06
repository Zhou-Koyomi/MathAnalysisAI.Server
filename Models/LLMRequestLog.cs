using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class LLMRequestLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        public int? AnalysisResultId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string ModelName { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string RequestType { get; set; } = string.Empty;

        public int? PromptTokenCount { get; set; }
        public int? CompletionTokenCount { get; set; }
        public int? TotalTokenCount { get; set; }
        public int? LatencyMs { get; set; }

        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = "unknown";

        [MaxLength(64)]
        public string? ErrorCode { get; set; }

        [MaxLength(1024)]
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AppUser? User { get; set; }
        public AnalysisResult? AnalysisResult { get; set; }
    }
}
