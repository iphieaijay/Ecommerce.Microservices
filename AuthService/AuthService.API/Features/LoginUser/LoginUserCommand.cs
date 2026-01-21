using MediatR;

namespace AuthService.API.Features.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<AuthResponse>;
