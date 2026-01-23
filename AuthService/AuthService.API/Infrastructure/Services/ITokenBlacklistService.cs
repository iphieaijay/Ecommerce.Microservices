namespace AuthService.Infrastructure.Services
{

    public interface ITokenBlacklistService
    {
        Task RevokeTokenAsync(string token, Guid userId, string reason, DateTime expiresAt);
        Task<bool> IsTokenRevokedAsync(string token);
        Task CleanupExpiredTokensAsync();
    }

}
