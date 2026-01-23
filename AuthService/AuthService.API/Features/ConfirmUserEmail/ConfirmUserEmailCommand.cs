using MediatR;

namespace AuthService.Features.ConfirmUserEmail
{
    public record ConfirmUserEmailCommand(
     Guid UserId,
     string Token
 ) : IRequest<ConfirmUserEmailResponse>;

}
