using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
