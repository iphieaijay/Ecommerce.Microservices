using AuthService.API.Features.ConfirmUserEmail;
using AuthService.API.Features.ForgotPassword;
using AuthService.API.Features.LoginUser;
using AuthService.API.Features.LogoutUser;
using AuthService.API.Features.RegisterUser;
using AuthService.API.Features.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        var confirmationLink = await _mediator.Send(command);

        var response = ApiResponse<object>.Ok(
            data: new { ConfirmationLink = confirmationLink },
            message: "User registered. Confirm email"
        );

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
    {
        var result = await _mediator.Send(command);

        var response = ApiResponse<object>.Ok(
            data: result,
            message: "Login successful"
        );

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutUserCommand command)
    {
        var result = await _mediator.Send(command);

        var response = ApiResponse<object>.Ok(
            data: result,
            message: "Logged out successfully"
        );

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        var result = await _mediator.Send(command);

        var response = ApiResponse<string>.Ok(
            data: result,
            message: "Password reset link sent if email exists"
        );

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        var result = await _mediator.Send(command);

        var response = ApiResponse<string>.Ok(
            data: result,
            message: "Password reset processed"
        );

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _mediator.Send(new ConfirmUserEmailCommand(userId, token));

        var response = ApiResponse<string>.Ok(
            data: result,
            message: "Email confirmation processed"
        );

        response.CorrelationId = HttpContext.TraceIdentifier;

        return Ok(response);
    }
}

