using MathAnalysisAI.Server.DTOs.Analysis;
using MathAnalysisAI.Server.Services.Analysis;
using MathAnalysisAI.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MathAnalysisAI.Server.Controllers;

[ApiController]
[Route("api/learning-analysis")]
public class LearningAnalysisController : ControllerBase
{
    private readonly AnalysisService _analysisService;
    private readonly IUserContext _userContext;
    private readonly ILogger<LearningAnalysisController> _logger;

    public LearningAnalysisController(
        AnalysisService analysisService,
        IUserContext userContext,
        ILogger<LearningAnalysisController> logger)
    {
        _analysisService = analysisService;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpPost("analyze")]
    [EnableRateLimiting("analyze")]
    public async Task<ActionResult<AnalysisResponseDto>> Analyze(
        [FromBody] AnalysisRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        var currentUser = await _userContext.GetCurrentUserAsync(cancellationToken);
        if (currentUser == null)
        {
            return Unauthorized(new { message = "Not logged in." });
        }

        var originalRequestUserId = request.UserId;
        if (originalRequestUserId.HasValue && originalRequestUserId.Value != currentUser.Id)
        {
            _logger.LogWarning(
                "Analyze blocked due to userId mismatch. RequestUserId={RequestUserId}, EffectiveUserId={EffectiveUserId}, Role={Role}",
                originalRequestUserId.Value,
                currentUser.Id,
                currentUser.Role);

            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden userId mismatch." });
        }

        request.UserId = currentUser.Id;

        try
        {
            var result = await _analysisService.AnalyzeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in learning analysis endpoint.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An internal server error occurred."
            });
        }
    }
}
