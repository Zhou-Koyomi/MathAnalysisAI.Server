using MathAnalysisAI.Server.DTOs.Knowledge;

namespace MathAnalysisAI.Server.DTOs.AnalysisContext;

public sealed class AnalysisContextDto
{
    public static AnalysisContextDto Empty { get; } = new();

    public IReadOnlyList<KnowledgeChunkContextDto> KnowledgeChunks { get; set; } = Array.Empty<KnowledgeChunkContextDto>();
    public IReadOnlyList<SymbolicEvidenceDto> SymbolicEvidences { get; set; } = Array.Empty<SymbolicEvidenceDto>();
    public string PromptContextBlock { get; set; } = string.Empty;
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public bool HasAnyContext { get; set; }
}
