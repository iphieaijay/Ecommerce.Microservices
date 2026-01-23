using AuthService.Infrastructure.EventBus;

namespace AuthService.Features.ConfirmUserEmail
{
    public record EmailConfirmedEvent(
    Guid UserId,
    string Email,
    DateTime ConfirmedAt
) : IEvent;
}
