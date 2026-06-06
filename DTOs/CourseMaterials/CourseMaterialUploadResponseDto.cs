namespace MathAnalysisAI.Server.DTOs.CourseMaterials
{
    public class CourseMaterialUploadResponseDto
    {
        public int MaterialId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ParseStatus { get; set; } = "pending";
        public string? ParseMessage { get; set; }
        public int ChunkCount { get; set; }
    }
}
