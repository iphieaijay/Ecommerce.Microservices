using AuthService.Domain;
using AuthService.Features.LoginUser;
using AuthService.Infrastructure.EventBus;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;


namespace AuthService.Features.LoginUser;
public class LoginUserHandler : IRequestHandler<LoginUserCommand, LoginUserResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LoginUserHandler> _logger;
    private readonly IConfiguration _configuration;

    public LoginUserHandler(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,ITokenService tokenService,
        IEventBus eventBus,ILogger<LoginUserHandler> logger,IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _eventBus = eventBus;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<LoginUserResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // Find user by email or username
        AppUser? user;
        if (request.EmailOrUsername.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(request.EmailOrUsername);
        }
        else
        {
            user = await _userManager.FindByNameAsync(request.EmailOrUsername);
        }

        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent user: {EmailOrUsername}", request.EmailOrUsername);
            throw new UnauthorizedAccessException("Invalid email/username or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
            throw new InvalidOperationException("Your account has been deactivated. Please contact support.");
        }

        // Check if email is confirmed
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login attempt with unconfirmed email: {UserId}", user.Id);
            throw new InvalidOperationException("Please confirm your email address before logging in. Check your inbox for the confirmation link.");
        }

        // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingTime = lockoutEnd - DateTimeOffset.UtcNow;

            _logger.LogWarning("Login attempt for locked out user: {UserId}", user.Id);
            throw new InvalidOperationException(
                $"Account is locked due to multiple failed login attempts. Please try again in {remainingTime?.Minutes} minutes.");
        }

        // Attempt sign in
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out: {UserId}", user.Id);
                throw new InvalidOperationException("Account locked due to multiple failed login attempts. Please try again later.");
            }

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var maxAttempts = _userManager.Options.Lockout.MaxFailedAccessAttempts;
            var attemptsLeft = maxAttempts - failedCount;

            _logger.LogWarning("Failed login attempt for user: {UserId}. Attempts left: {AttemptsLeft}", user.Id, attemptsLeft);

            throw new UnauthorizedAccessException(
                attemptsLeft > 0
                    ? $"Invalid password. {attemptsLeft} attempt(s) remaining before account lockout."
                    : "Invalid email/username or password");
        }

        // Reset failed access count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Generate tokens
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Save refresh token
        var refreshTokenExpiry = request.RememberMe
            ? DateTime.UtcNow.AddDays(30)
            : DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = refreshTokenExpiry;
        user.LastLoginAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        // Calculate access token expiration
        var accessTokenExpiration = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["JwtSettings:AccessTokenExpirationMinutes"] ?? "60"));

        // Publish domain event
        await _eventBus.PublishAsync(new UserLoggedInEvent(
            user.Id,
            user.Email ?? string.Empty,
            DateTime.UtcNow,
            request.RememberMe
        ), cancellationToken);

        _logger.LogInformation("User logged in successfully: {UserId}, {Email}", user.Id, user.Email);

        return new LoginUserResponse(
            accessToken,
            refreshToken,
            accessTokenExpiration,
            new UserInfo(
                user.Id,
                user.Email ?? string.Empty,
                user.UserName ?? string.Empty,
                user.FirstName,
                user.LastName,
                roles
            )
        );
    }
}