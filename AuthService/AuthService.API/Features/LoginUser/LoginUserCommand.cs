using MediatR;

namespace AuthService.Features.LoginUser;

public record LoginUserCommand(string EmailOrUsername, string Password, bool RememberMe = false
) : IRequest<LoginUserResponse>;
