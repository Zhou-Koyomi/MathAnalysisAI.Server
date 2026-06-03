namespace MathAnalysisAI.Server.Services.OCR
{
    public class PhotoSolutionOcrRequest
    {
        public int CourseId { get; set; }
        public int? ChapterId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
        public string? UserHint { get; set; }
    }
}
