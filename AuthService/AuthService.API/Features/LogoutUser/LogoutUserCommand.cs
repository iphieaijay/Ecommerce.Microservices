using MediatR;

namespace AuthService.API.Features.LogoutUser
{
    public sealed class LogoutUserCommand : IRequest<string>
    {
        public string RefreshToken { get; set; } = null!;
    }

   
}
