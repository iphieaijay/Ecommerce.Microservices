using AuthService.Domain;
using System.Security.Claims;

namespace AuthService.Infrastructure.Services
{
    public interface ITokenService
    {
        Task<string> GenerateAccessTokenAsync(AppUser user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        Task<bool> ValidateTokenAsync(string token);
    }

}
