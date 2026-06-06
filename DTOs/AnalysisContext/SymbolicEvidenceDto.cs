namespace MathAnalysisAI.Server.DTOs.AnalysisContext;

public sealed class SymbolicEvidenceDto
{
    public SymbolicTaskDto? Task { get; set; }
    public bool Success { get; set; }
    public string? ResultText { get; set; }
    public string? ResultLatex { get; set; }
    public string? ErrorCode { get; set; }
    public string? Warning { get; set; }
    public int? ElapsedMs { get; set; }
    public bool UsedInPrompt { get; set; }
}
