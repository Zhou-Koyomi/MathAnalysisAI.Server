namespace MathAnalysisAI.Server.DTOs.CourseMaterials
{
    public class CourseMaterialListItemDto
    {
        public int MaterialId { get; set; }
        public int CourseId { get; set; }
        public int? ChapterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MaterialKind { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ParseStatus { get; set; } = string.Empty;
        public string? ParseMessage { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ParsedAt { get; set; }
    }
}
