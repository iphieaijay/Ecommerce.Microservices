using AuthService.Domain;
using AuthService.Features.LogoutUser;
using AuthService.Infrastructure.EventBus;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;

public class LogoutUserHandler : IRequestHandler<LogoutUserCommand, LogoutUserResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LogoutUserHandler> _logger;

    public LogoutUserHandler(UserManager<AppUser> userManager, ITokenBlacklistService tokenBlacklistService,
        IEventBus eventBus, ILogger<LogoutUserHandler> logger)
    {
        _userManager = userManager;
        _tokenBlacklistService = tokenBlacklistService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<LogoutUserResponse> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        // Find user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Logout attempt for non-existent user: {UserId}", request.UserId);
            throw new KeyNotFoundException("User not found");
        }

        // Parse token to get expiration
        var tokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken? jwtToken = null;

        try
        {
            jwtToken = tokenHandler.ReadJwtToken(request.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid token format during logout for user: {UserId}", request.UserId);
            throw new InvalidOperationException("Invalid token format");
        }

        var tokenExpiration = jwtToken.ValidTo;

        // Add token to blacklist
        await _tokenBlacklistService.RevokeTokenAsync(request.AccessToken,request.UserId,"User logout", tokenExpiration);

        // Clear refresh token
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        // Publish domain event
        await _eventBus.PublishAsync(new UserLoggedOutEvent(user.Id,user.Email ?? string.Empty,DateTime.UtcNow), cancellationToken);

        _logger.LogInformation("User logged out successfully: {UserId}, {Email}", user.Id, user.Email);

        return new LogoutUserResponse(true, "Logged out successfully" );
    }
}
