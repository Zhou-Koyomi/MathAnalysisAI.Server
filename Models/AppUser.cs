using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? RealName { get; set; }

        [MaxLength(64)]
        public string? StudentNumber { get; set; }

        [Required]
        [MaxLength(32)]
        public string Role { get; set; } = AppUserRole.Student;

        [MaxLength(128)]
        public string? SchoolName { get; set; }

        [MaxLength(128)]
        public string? DepartmentName { get; set; }

        [MaxLength(128)]
        public string? ClassName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StudentSolution> StudentSolutions { get; set; } = new List<StudentSolution>();
        public ICollection<UserKnowledgeState> UserKnowledgeStates { get; set; } = new List<UserKnowledgeState>();
        public ICollection<UserCourseStats> UserCourseStats { get; set; } = new List<UserCourseStats>();
    }
}
