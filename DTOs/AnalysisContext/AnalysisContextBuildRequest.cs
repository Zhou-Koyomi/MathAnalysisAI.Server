using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.DTOs.AnalysisContext;

public sealed class AnalysisContextBuildRequest
{
    public required AnalysisRequestDto Request { get; set; }
    public required Course Course { get; set; }
    public Chapter? Chapter { get; set; }
    public required Problem Problem { get; set; }
    public StudentSolution? StudentSolution { get; set; }
    public IReadOnlyList<string>? NormalizedKnowledgePointCodes { get; set; }
}
