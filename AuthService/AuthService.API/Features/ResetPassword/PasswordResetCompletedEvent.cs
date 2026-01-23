using AuthService.Infrastructure.EventBus;

namespace AuthService.Features.ResetPassword
{
    public record PasswordResetCompletedEvent(Guid UserId,string Email, DateTime ResetAt) : IEvent;
}
