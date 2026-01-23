using AuthService.Domain;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Features.RefreshToken
{
    public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ILogger<RefreshTokenHandler> _logger;
        private readonly IConfiguration _configuration;

        public RefreshTokenHandler(
            UserManager<AppUser> userManager,
            ITokenService tokenService,
            ITokenBlacklistService tokenBlacklistService,
            ILogger<RefreshTokenHandler> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _tokenBlacklistService = tokenBlacklistService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            // Validate and get principal from expired token
            var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                _logger.LogWarning("Invalid access token provided for refresh");
                throw new UnauthorizedAccessException("Invalid access token");
            }

            // Get user ID from claims
            var userIdClaim = principal.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("User ID not found in token claims");
                throw new UnauthorizedAccessException("Invalid token claims");
            }

            // Check if old token is blacklisted
            if (await _tokenBlacklistService.IsTokenRevokedAsync(request.AccessToken))
            {
                _logger.LogWarning("Attempted to refresh a revoked token for user: {UserId}", userId);
                throw new UnauthorizedAccessException("Token has been revoked");
            }

            // Find user
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("Refresh token attempt for non-existent user: {UserId}", userId);
                throw new UnauthorizedAccessException("User not found");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token attempt for inactive user: {UserId}", user.Id);
                throw new InvalidOperationException("Your account has been deactivated");
            }

            // Validate refresh token
            if (user.RefreshToken != request.RefreshToken)
            {
                _logger.LogWarning("Invalid refresh token for user: {UserId}", user.Id);
                throw new UnauthorizedAccessException("Invalid refresh token");
            }

            // Check if refresh token is expired
            if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Expired refresh token for user: {UserId}", user.Id);
                throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");
            }

            // Blacklist old access token
            var jwtToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(request.AccessToken);
            await _tokenBlacklistService.RevokeTokenAsync(
                request.AccessToken,
                user.Id,
                "Token refreshed",
                jwtToken.ValidTo);

            // Get user roles
            var roles = await _userManager.GetRolesAsync(user);

            // Generate new tokens
            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Update user's refresh token
            user.RefreshToken = newRefreshToken;
            // Extend refresh token expiry
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _userManager.UpdateAsync(user);

            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60"));

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

            return new RefreshTokenResponse(
                newAccessToken,
                newRefreshToken,
                accessTokenExpiration
            );
        }
    }
}
