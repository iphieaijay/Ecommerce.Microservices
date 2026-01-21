using AuthService.API.Domain;

namespace AuthService.API.Common.JWT
{
    public interface ITokenService
    {
        Task<string> GenerateAccessToken(AppUser user, IList<string> roles);
        Task<RefreshToken> GenerateRefreshToken(AppUser user);
        Task RevokeRefreshToken(string token);
    }

}
