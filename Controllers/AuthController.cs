using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.Auth;
using MathAnalysisAI.Server.Models;
using MathAnalysisAI.Server.Services.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string SessionUserIdKey = "auth_user_id";

    private readonly ApplicationDbContext _db;
    private readonly IUserContext _userContext;

    public AuthController(
        ApplicationDbContext db,
        IUserContext userContext)
    {
        _db = db;
        _userContext = userContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await _userContext.GetCurrentUserAsync(cancellationToken);
        if (user != null)
        {
            return Ok(MapToCurrentUser(user));
        }

        return Unauthorized(new { message = "Not logged in." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<CurrentUserDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        var user = await _db.AppUsers
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username." });
        }

        HttpContext.Session.SetInt32(SessionUserIdKey, user.Id);
        return Ok(MapToCurrentUser(user));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(SessionUserIdKey);
        return Ok(new { success = true });
    }

    private static CurrentUserDto MapToCurrentUser(AppUser user)
    {
        return new CurrentUserDto
        {
            UserId = user.Id,
            Username = user.Username,
            RealName = user.RealName,
            Role = user.Role,
            SchoolName = user.SchoolName,
            DepartmentName = user.DepartmentName,
            ClassName = user.ClassName
        };
    }
}
