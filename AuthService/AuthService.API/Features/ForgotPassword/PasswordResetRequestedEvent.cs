using AuthService.Infrastructure.EventBus;

namespace AuthService.Features.ForgotPassword
{
    // Domain Event
    public record PasswordResetRequestedEvent(
        Guid UserId,
        string Email,
        DateTime RequestedAt
    ) : IEvent;
}
