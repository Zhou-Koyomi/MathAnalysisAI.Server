using MathAnalysisAI.Server.DTOs.PhotoSolutions;

namespace MathAnalysisAI.Server.Services.OCR
{
    public interface IPhotoSolutionOcrProvider
    {
        Task<PhotoSolutionOcrResponseDto> RecognizeAsync(
            PhotoSolutionOcrRequest request,
            CancellationToken cancellationToken = default);
    }
}
