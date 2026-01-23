using MediatR;

namespace AuthService.Features.RefreshToken
{
    public record RefreshTokenCommand(string AccessToken,string RefreshToken) : IRequest<RefreshTokenResponse>;
}
