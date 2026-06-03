using MathAnalysisAI.Server.DTOs.Visualization;

namespace MathAnalysisAI.Server.Services.Visualization
{
    public interface IGeoGebraCommandValidator
    {
        GeoGebraValidationResultDto Validate(IEnumerable<string>? commands);
    }
}
