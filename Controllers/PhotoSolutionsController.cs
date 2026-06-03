using MathAnalysisAI.Server.DTOs.PhotoSolutions;
using MathAnalysisAI.Server.Services.OCR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MathAnalysisAI.Server.Controllers
{
    [ApiController]
    [Route("api/photo-solutions")]
    public class PhotoSolutionsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private readonly IPhotoSolutionOcrProvider _ocrProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PhotoSolutionsController> _logger;

        public PhotoSolutionsController(
            IPhotoSolutionOcrProvider ocrProvider,
            IConfiguration configuration,
            ILogger<PhotoSolutionsController> logger)
        {
            _ocrProvider = ocrProvider;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("ocr")]
        [EnableRateLimiting("ocr")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<ActionResult<PhotoSolutionOcrResponseDto>> Ocr(
            [FromForm] int courseId,
            [FromForm] int? chapterId,
            [FromForm] string? userHint,
            [FromForm] IFormFile? file,
            CancellationToken cancellationToken)
        {
            if (courseId <= 0)
            {
                return BadRequest("courseId is required.");
            }

            if (file == null || file.Length <= 0)
            {
                return BadRequest("file is required.");
            }

            var maxImageBytes = _configuration.GetValue<int?>("PhotoSolutionOcr:MaxImageBytes") ?? (10 * 1024 * 1024);
            if (file.Length > maxImageBytes)
            {
                return BadRequest($"file size exceeds {maxImageBytes} bytes limit.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest("Only image files are supported in this stage: jpg/jpeg/png/webp.");
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest("Unsupported image content type.");
            }

            byte[] imageBytes;
            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                imageBytes = ms.ToArray();
            }

            var request = new PhotoSolutionOcrRequest
            {
                CourseId = courseId,
                ChapterId = chapterId,
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType ?? "image/jpeg",
                ImageBytes = imageBytes,
                UserHint = userHint
            };

            try
            {
                var result = await _ocrProvider.RecognizeAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Photo solution OCR provider request failed.");
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "OCR provider request failed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Photo solution OCR failed unexpectedly.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Photo solution OCR failed."
                });
            }
        }
    }
}
