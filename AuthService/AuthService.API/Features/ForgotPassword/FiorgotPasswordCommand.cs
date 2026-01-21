using MediatR;
using Shared.Contracts.Responses;

namespace AuthService.API.Features.ForgotPassword
{
    public sealed class ForgotPasswordCommand : IRequest<string>
    {
        public string Email { get; set; } = null!;
    }

}
