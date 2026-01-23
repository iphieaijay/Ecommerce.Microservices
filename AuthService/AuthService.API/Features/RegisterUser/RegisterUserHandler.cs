using AuthService.Domain;
using AuthService.Features.RegisterUser;
using AuthService.Infrastructure.EventBus;
using AuthService.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<RegisterUserHandler> _logger;
    private readonly IConfiguration _configuration;

    public RegisterUserHandler(UserManager<AppUser> userManager, IEmailService emailService,
        IEventBus eventBus,ILogger<RegisterUserHandler> logger,IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _eventBus = eventBus;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            throw new InvalidOperationException("Email is already registered");
        }

        // Check if username already exists
        var existingUserByUsername = await _userManager.FindByNameAsync(request.UserName);
        if (existingUserByUsername != null)
        {
            _logger.LogWarning("Registration attempt with existing username: {UserName}", request.UserName);
            throw new InvalidOperationException("Username is already taken");
        }

        // Create new user
        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber=request.PhoneNo,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Address=request.Address,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user {Email}: {Errors}", request.Email, errors);
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Assign default "User" role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate email confirmation token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        _logger.LogDebug("Generated token (first 50 chars): {Token}",
            token.Length > 50 ? token.Substring(0, 50) : token);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        
        // Create confirmation link
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:7078";
        var confirmationLink = $"{baseUrl}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";

        // Send confirmation email
        await _emailService.SendEmailConfirmationAsync(user.Email, confirmationLink);

        // Publish domain event
        await _eventBus.PublishAsync(new UserRegisteredEvent(
            user.Id,
            user.Email,
            user.UserName,
            user.FirstName,
            user.LastName,
            DateTime.UtcNow
        ), cancellationToken);

        _logger.LogInformation("User registered successfully: {UserId}, {Email}", user.Id, user.Email);

        return new RegisterUserResponse(user.Id, user.Email,user.UserName,"Registration successful. Please check your email to confirm your account.");
    }
}
