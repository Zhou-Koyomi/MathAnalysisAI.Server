namespace MathAnalysisAI.Server.DTOs.Symbolic;

public sealed class SymbolicComputeResponseDto
{
    public bool Success { get; set; }
    public string? Operation { get; set; }
    public string? Input { get; set; }
    public string? ResultText { get; set; }
    public string? ResultLatex { get; set; }
    public object? ResultJson { get; set; }
    public string? Engine { get; set; }
    public string? EngineVersion { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int ElapsedMs { get; set; }
}
