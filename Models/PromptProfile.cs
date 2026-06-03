using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class PromptProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Mode { get; set; } = "review_solution";

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string SystemPrompt { get; set; } = string.Empty;

        [Required]
        public string UserPromptTemplate { get; set; } = string.Empty;

        [Required]
        public string OutputSchemaJson { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string Version { get; set; } = "v1";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Course? Course { get; set; }
    }
}
