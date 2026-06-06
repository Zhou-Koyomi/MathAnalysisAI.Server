namespace MathAnalysisAI.Server.DTOs.AnalysisContext;

public sealed class SymbolicTaskDto
{
    public string? Operation { get; set; }
    public string? Expression { get; set; }
    public string? Variable { get; set; }
    public string? Lower { get; set; }
    public string? Upper { get; set; }
    public string? Point { get; set; }
    public int? Order { get; set; }
    public string? Reason { get; set; }
    public double? Confidence { get; set; }
    public string? Source { get; set; }
}
