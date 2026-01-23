using AuthService.Infrastructure.EventBus;

namespace AuthService.Features.LogoutUser
{
    public record UserLoggedOutEvent(Guid UserId,string Email,DateTime LoggedOutAt) : IEvent;
}
