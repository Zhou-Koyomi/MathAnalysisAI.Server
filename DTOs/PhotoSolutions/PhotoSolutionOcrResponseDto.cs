namespace MathAnalysisAI.Server.DTOs.PhotoSolutions
{
    public class PhotoSolutionOcrResponseDto
    {
        public string ProblemText { get; set; } = string.Empty;
        public string StudentSolutionText { get; set; } = string.Empty;
        public List<DetectedSectionDto> DetectedSections { get; set; } = new();
        public List<FormulaCandidateDto> Formulas { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal? Confidence { get; set; }
        public string? RawProvider { get; set; }
        public string? ModelName { get; set; }
    }
}
