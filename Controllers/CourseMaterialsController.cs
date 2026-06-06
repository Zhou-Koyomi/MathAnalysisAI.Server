using MathAnalysisAI.Server.DTOs.CourseMaterials;
using MathAnalysisAI.Server.DTOs.Knowledge;
using MathAnalysisAI.Server.Services.Auth;
using MathAnalysisAI.Server.Services.Materials;
using MathAnalysisAI.Server.Services.Knowledge;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MathAnalysisAI.Server.Data;

namespace MathAnalysisAI.Server.Controllers
{
    [ApiController]
    [Route("api/course-materials")]
    public class CourseMaterialsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly CourseMaterialIngestionService _ingestionService;
        private readonly IKnowledgeRetrievalService _knowledgeRetrievalService;
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CourseMaterialsController> _logger;

        public CourseMaterialsController(
            ApplicationDbContext db,
            CourseMaterialIngestionService ingestionService,
            IKnowledgeRetrievalService knowledgeRetrievalService,
            IUserContext userContext,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<CourseMaterialsController> logger)
        {
            _db = db;
            _ingestionService = ingestionService;
            _knowledgeRetrievalService = knowledgeRetrievalService;
            _userContext = userContext;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<CourseMaterialListItemDto>>> List(
            [FromQuery] int courseId,
            [FromQuery] int? chapterId,
            [FromQuery] string? parseStatus,
            [FromQuery] int? take,
            CancellationToken cancellationToken)
        {
            var authResult = await EnsureMaterialManagerAsync(cancellationToken);
            if (authResult != null)
            {
                return authResult;
            }

            if (courseId <= 0)
            {
                return BadRequest("courseId is required.");
            }

            var safeTake = take ?? 50;
            if (safeTake <= 0)
            {
                safeTake = 50;
            }
            if (safeTake > 100)
            {
                safeTake = 100;
            }

            var query = _db.CourseMaterials
                .AsNoTracking()
                .Where(x => x.CourseId == courseId);

            if (chapterId.HasValue)
            {
                var selectedChapterId = chapterId.Value;
                query = query.Where(x => _db.MaterialChunks.Any(c => c.CourseMaterialId == x.Id && c.ChapterId == selectedChapterId));
            }

            if (!string.IsNullOrWhiteSpace(parseStatus))
            {
                var normalizedStatus = parseStatus.Trim().ToLowerInvariant();
                query = query.Where(x => x.ParseStatus.ToLower() == normalizedStatus);
            }

            var items = await query
                .OrderByDescending(x => x.UploadedAt)
                .Take(safeTake)
                .Select(x => new CourseMaterialListItemDto
                {
                    MaterialId = x.Id,
                    CourseId = x.CourseId,
                    ChapterId = _db.MaterialChunks
                        .Where(c => c.CourseMaterialId == x.Id && c.ChapterId != null)
                        .OrderBy(c => c.ChunkIndex)
                        .Select(c => c.ChapterId)
                        .FirstOrDefault(),
                    Title = x.Title,
                    MaterialKind = x.MaterialKind,
                    OriginalFileName = x.OriginalFileName,
                    FileExtension = x.FileExtension,
                    FileSizeBytes = x.FileSizeBytes,
                    ParseStatus = x.ParseStatus,
                    ParseMessage = x.ParseMessage,
                    ChunkCount = x.Chunks.Count,
                    UploadedAt = x.UploadedAt,
                    ParsedAt = x.ParsedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(items);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IReadOnlyList<KnowledgeChunkContextDto>>> Search(
            [FromQuery] int courseId,
            [FromQuery] int? chapterId,
            [FromQuery] string? q,
            [FromQuery] string? studentSolutionText,
            [FromQuery] int? topK,
            CancellationToken cancellationToken)
        {
            var authResult = await EnsureMaterialManagerAsync(cancellationToken);
            if (authResult != null)
            {
                return authResult;
            }

            if (courseId <= 0)
            {
                return BadRequest("courseId is required.");
            }

            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("q is required.");
            }

            var safeTopK = Math.Clamp(topK ?? 3, 1, 8);

            var request = new KnowledgeRetrievalRequest
            {
                CourseId = courseId,
                ChapterId = chapterId,
                ProblemText = q.Trim(),
                StudentSolutionText = string.IsNullOrWhiteSpace(studentSolutionText) ? null : studentSolutionText.Trim(),
                TopK = safeTopK
            };

            var result = await _knowledgeRetrievalService.RetrieveAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("upload")]
        [RequestSizeLimit(100 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
        public async Task<ActionResult<CourseMaterialUploadResponseDto>> Upload(
            [FromForm] int courseId,
            [FromForm] int? chapterId,
            [FromForm] string? title,
            [FromForm] string? materialKind,
            [FromForm] string? visibility,
            [FromForm] IFormFile? file,
            CancellationToken cancellationToken)
        {
            var authResult = await EnsureMaterialManagerAsync(cancellationToken);
            if (authResult != null)
            {
                return authResult;
            }

            if (file == null || file.Length <= 0)
            {
                return BadRequest("file is required.");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                return BadRequest("当前阶段仅支持 PDF。");
            }

            try
            {
                var result = await _ingestionService.IngestPdfAsync(
                    courseId,
                    chapterId,
                    title,
                    materialKind,
                    visibility,
                    file,
                    uploadedByUserId: null,
                    cancellationToken: cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Course material upload failed unexpectedly.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Course material upload failed."
                });
            }
        }

        private async Task<ActionResult?> EnsureMaterialManagerAsync(CancellationToken cancellationToken)
        {
            if (ShouldAllowDevelopmentOverride())
            {
                return null;
            }

            var user = await _userContext.GetCurrentUserAsync(cancellationToken);
            if (user == null)
            {
                return Unauthorized(new { message = "Not logged in." });
            }

            var role = user.Role?.Trim();
            if (string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden." });
        }

        private bool ShouldAllowDevelopmentOverride()
        {
            var enabled = _configuration.GetValue<bool>("Auth:EnableDevelopmentMaterialAccessOverride");
            return enabled && _environment.IsDevelopment();
        }
    }
}
