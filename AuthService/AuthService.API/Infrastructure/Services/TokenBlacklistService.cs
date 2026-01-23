using AuthService.Domain;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AuthService.Infrastructure.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly AuthDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenBlacklistService> _logger;
    private const string CacheKeyPrefix = "revoked_token_";

    public TokenBlacklistService(
        AuthDbContext context,
        IMemoryCache cache,
        ILogger<TokenBlacklistService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task RevokeTokenAsync(string token, Guid userId, string reason, DateTime expiresAt)
    {
        try
        {
            // Check if token is already revoked
            var existingToken = await _context.RevokedTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (existingToken != null)
            {
                _logger.LogWarning("Token already revoked for user {UserId}", userId);
                return;
            }

            var revokedToken = new RevokedToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                UserId = userId,
                RevokedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                Reason = reason
            };

            _context.RevokedTokens.Add(revokedToken);
            await _context.SaveChangesAsync();

            // Add to cache for fast lookup
            var cacheKey = $"{CacheKeyPrefix}{token}";
            var cacheExpiration = expiresAt - DateTime.UtcNow;

            if (cacheExpiration.TotalSeconds > 0)
            {
                _cache.Set(cacheKey, true, cacheExpiration);
            }

            _logger.LogInformation("Token revoked for user {UserId}. Reason: {Reason}", userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsTokenRevokedAsync(string token)
    {
        try
        {
            // Check cache first for performance
            var cacheKey = $"{CacheKeyPrefix}{token}";
            if (_cache.TryGetValue(cacheKey, out bool _))
            {
                return true;
            }

            // Check database
            var isRevoked = await _context.RevokedTokens
                .AnyAsync(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

            if (isRevoked)
            {
                // Add to cache for future lookups
                _cache.Set(cacheKey, true, TimeSpan.FromHours(1));
            }

            return isRevoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is revoked");
            // In case of error, assume token is valid to avoid blocking users
            return false;
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            var expiredTokens = await _context.RevokedTokens
                .Where(t => t.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.RevokedTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired tokens");
        }
    }
}
