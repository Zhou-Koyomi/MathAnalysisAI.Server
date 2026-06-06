using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Services.Auth;

public interface IUserContext
{
    Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<int?> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
    Task<string?> GetCurrentRoleAsync(CancellationToken cancellationToken = default);
    Task<bool> IsInRoleAsync(string role, CancellationToken cancellationToken = default);
    Task<bool> IsInAnyRoleAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken = default);
}
