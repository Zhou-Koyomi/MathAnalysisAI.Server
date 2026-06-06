using MathAnalysisAI.Server.DTOs.Symbolic;

namespace MathAnalysisAI.Server.Services.Symbolic;

public interface ISymbolicMathService
{
    Task<SymbolicComputeResponseDto> ComputeAsync(
        SymbolicComputeRequestDto request,
        CancellationToken cancellationToken = default);
}
