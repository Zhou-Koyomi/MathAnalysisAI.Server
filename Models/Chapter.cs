using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class Chapter
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        [MaxLength(512)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Course? Course { get; set; }
        public ICollection<KnowledgePoint> KnowledgePoints { get; set; } = new List<KnowledgePoint>();
        public ICollection<Problem> Problems { get; set; } = new List<Problem>();
    }
}
