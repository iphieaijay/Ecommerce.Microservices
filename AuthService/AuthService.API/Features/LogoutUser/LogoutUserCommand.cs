using MediatR;

namespace AuthService.Features.LogoutUser
{
    public record LogoutUserCommand(string AccessToken, Guid UserId) : IRequest<LogoutUserResponse>;


}


