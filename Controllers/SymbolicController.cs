using MathAnalysisAI.Server.DTOs.Symbolic;
using MathAnalysisAI.Server.Models;
using MathAnalysisAI.Server.Services.Auth;
using MathAnalysisAI.Server.Services.Symbolic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MathAnalysisAI.Server.Controllers;

[ApiController]
[Route("api/symbolic")]
public class SymbolicController : ControllerBase
{
    private readonly ISymbolicMathService _symbolicMathService;
    private readonly IUserContext _userContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SymbolicController> _logger;

    public SymbolicController(
        ISymbolicMathService symbolicMathService,
        IUserContext userContext,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<SymbolicController> logger)
    {
        _symbolicMathService = symbolicMathService;
        _userContext = userContext;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("compute")]
    [EnableRateLimiting("symbolic")]
    public async Task<ActionResult<SymbolicComputeResponseDto>> Compute(
        [FromBody] SymbolicComputeRequestDto? request,
        CancellationToken cancellationToken)
    {
        var authResult = await EnsureSymbolicAccessAsync(cancellationToken);
        if (authResult != null)
        {
            return authResult;
        }

        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Operation))
        {
            return BadRequest(new { message = "Field 'operation' is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Expression))
        {
            return BadRequest(new { message = "Field 'expression' is required." });
        }

        var response = await _symbolicMathService.ComputeAsync(request, cancellationToken);
        return Ok(response);
    }

    private async Task<ActionResult?> EnsureSymbolicAccessAsync(CancellationToken cancellationToken)
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

        if (string.Equals(user.Role?.Trim(), AppUserRole.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _logger.LogWarning(
            "Forbidden symbolic compute access. UserId={UserId}, Role={Role}",
            user.Id,
            user.Role);

        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Forbidden." });
    }

    private bool ShouldAllowDevelopmentOverride()
    {
        var enabled = _configuration.GetValue<bool>("Auth:EnableDevelopmentSymbolicAccessOverride");
        return enabled && _environment.IsDevelopment();
    }
}
