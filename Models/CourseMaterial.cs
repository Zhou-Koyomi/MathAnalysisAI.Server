using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Server.Models
{
    public class CourseMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string MaterialKind { get; set; } = "other";

        [Required]
        [MaxLength(16)]
        public string Language { get; set; } = "zh-CN";

        [MaxLength(128)]
        public string? Author { get; set; }

        [MaxLength(64)]
        public string? Edition { get; set; }

        [MaxLength(128)]
        public string? Publisher { get; set; }

        [Required]
        [MaxLength(32)]
        public string Visibility { get; set; } = "course_internal";

        [MaxLength(1024)]
        public string? CopyrightNote { get; set; }

        [Required]
        [MaxLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string FileExtension { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        [MaxLength(128)]
        public string? FileHash { get; set; }

        [Required]
        [MaxLength(512)]
        public string StoragePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string ParseStatus { get; set; } = "pending";

        [MaxLength(1024)]
        public string? ParseMessage { get; set; }

        public int? UploadedByUserId { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ParsedAt { get; set; }

        public Course? Course { get; set; }
        public AppUser? UploadedByUser { get; set; }
        public ICollection<MaterialChunk> Chunks { get; set; } = new List<MaterialChunk>();
    }
}
