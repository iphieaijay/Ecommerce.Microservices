using AuthService.Features.ForgotPassword;
using AuthService.Features.ResetPassword;
using AuthService.Features.ConfirmUserEmail;
using AuthService.Features.LoginUser;
using AuthService.Features.LogoutUser;
using AuthService.Features.RefreshToken;
using AuthService.Features.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.API.Controllers;

/// <summary>
/// Controller for user authentication and account management operations.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator instance for sending commands and queries.</param>
    /// <param name="logger">The logger instance for logging controller operations.</param>
    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="command">The registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration result with user details.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Invalid registration data.</response>
    /// <response code="409">Email or username already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user and generates access tokens.
    /// </summary>
    /// <param name="command">The login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, refresh token, and user details.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="400">Invalid login data or unconfirmed email.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginUserResponse>> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Logs out the current user and revokes their access token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Logout confirmation.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">Unauthorized - user not authenticated.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(LogoutUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LogoutUserResponse>> Logout(CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user claims" });
            }

            // Get access token from Authorization header
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(accessToken))
            {
                return Unauthorized(new { error = "Access token not provided" });
            }

            var command = new LogoutUserCommand(accessToken, userId);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirms a user's email address.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="token">The email confirmation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation result.</returns>
    /// <response code="200">Email confirmed successfully.</response>
    /// <response code="400">Invalid or expired token.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ConfirmUserEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmUserEmailResponse>> ConfirmEmail(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ConfirmUserEmailCommand(userId, token);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Initiates a password reset process by sending a reset link to the user's email.
    /// </summary>
    /// <param name="command">The forgot password request with email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Password reset email sent (or would be sent if email exists).</response>
    /// <response code="400">Invalid email format.</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// </summary>
    /// <param name="command">The reset password request with email, token, and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Password reset confirmation.</returns>
    /// <response code="200">Password reset successfully.</response>
    /// <response code="400">Invalid or expired token, or invalid password format.</response>
    /// <response code="404">User not found.</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        [FromBody] ResetPasswordCommand command,  CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="command">The refresh token request with access token and refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New access token and refresh token.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Invalid or expired refresh token.</response>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    /// <returns>Current user's details.</returns>
    /// <response code="200">User information retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("userId")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var firstName = User.FindFirst("firstName")?.Value;
        var lastName = User.FindFirst("lastName")?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new
        {
            userId,
            email,
            userName,
            firstName,
            lastName,
            roles
        });
    }
}