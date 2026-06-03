using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Auth;

public class CurrentUserService : IUserContext
{
    private const string SessionUserIdKey = "auth_user_id";

    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public CurrentUserService(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var sessionUserId = httpContext.Session.GetInt32(SessionUserIdKey);
        if (sessionUserId.HasValue)
        {
            var sessionUser = await _db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sessionUserId.Value, cancellationToken);

            if (sessionUser != null)
            {
                return sessionUser;
            }

            httpContext.Session.Remove(SessionUserIdKey);
        }

        if (!ShouldUseDevelopmentFallback())
        {
            return null;
        }

        var fallbackUsername = _configuration["Auth:DevelopmentFallbackUser"];
        if (string.IsNullOrWhiteSpace(fallbackUsername))
        {
            return null;
        }

        var fallbackUser = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == fallbackUsername, cancellationToken);

        return fallbackUser;
    }

    public async Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Id;
    }

    public async Task<string?> GetCurrentRoleAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.Role;
    }

    public async Task<bool> IsInRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        var currentRole = await GetCurrentRoleAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(currentRole))
        {
            return false;
        }

        return string.Equals(currentRole, role, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsInAnyRoleAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        if (roles == null)
        {
            return false;
        }

        var currentRole = await GetCurrentRoleAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(currentRole))
        {
            return false;
        }

        return roles.Any(role => !string.IsNullOrWhiteSpace(role)
                                 && string.Equals(role, currentRole, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user != null;
    }

    private bool ShouldUseDevelopmentFallback()
    {
        var enabled = _configuration.GetValue<bool>("Auth:EnableDevelopmentFallback");
        return enabled && _environment.IsDevelopment();
    }
}
