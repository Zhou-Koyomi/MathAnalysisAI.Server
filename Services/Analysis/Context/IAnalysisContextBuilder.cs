using MathAnalysisAI.Server.DTOs.AnalysisContext;

namespace MathAnalysisAI.Server.Services.Analysis.Context;

public interface IAnalysisContextBuilder
{
    Task<AnalysisContextDto> BuildAsync(
        AnalysisContextBuildRequest request,
        CancellationToken cancellationToken = default);
}
