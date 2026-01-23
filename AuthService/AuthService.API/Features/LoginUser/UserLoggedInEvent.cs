using AuthService.Infrastructure.EventBus;

namespace AuthService.Features.LoginUser
{
    public record UserLoggedInEvent(
    Guid UserId,
    string Email,
    DateTime LoggedInAt,
    bool RememberMe
) : IEvent;
}
