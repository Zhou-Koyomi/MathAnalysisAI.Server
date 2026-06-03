using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class AnalysisVisualization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AnalysisResultId { get; set; }

        [Required]
        [MaxLength(32)]
        public string Engine { get; set; } = "geogebra";

        [MaxLength(64)]
        public string? VisualizationType { get; set; }

        public string? CommandsJson { get; set; }
        public string? ViewConfigJson { get; set; }
        public string? StepBindingJson { get; set; }

        [MaxLength(512)]
        public string? Caption { get; set; }

        [MaxLength(32)]
        public string? ValidationStatus { get; set; }

        [MaxLength(1024)]
        public string? ValidationMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AnalysisResult? AnalysisResult { get; set; }
    }
}
