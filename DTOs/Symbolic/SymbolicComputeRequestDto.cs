namespace MathAnalysisAI.Server.DTOs.Symbolic;

public sealed class SymbolicComputeRequestDto
{
    public string? Operation { get; set; }
    public string? Expression { get; set; }
    public string? Variable { get; set; }
    public string? Lower { get; set; }
    public string? Upper { get; set; }
    public string? Point { get; set; }
    public int? Order { get; set; }
    public Dictionary<string, object?>? Assumptions { get; set; }
    public string? InputFormat { get; set; }
    public string? OutputFormat { get; set; }
    public int? TimeoutMs { get; set; }
}
