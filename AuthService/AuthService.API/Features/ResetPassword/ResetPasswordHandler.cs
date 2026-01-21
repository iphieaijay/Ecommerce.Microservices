using MediatR;
using Shared.Contracts.Responses;

namespace AuthService.API.Features.ResetPassword
{
    public sealed class ResetPasswordCommand : IRequest<string>
    {
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

}
