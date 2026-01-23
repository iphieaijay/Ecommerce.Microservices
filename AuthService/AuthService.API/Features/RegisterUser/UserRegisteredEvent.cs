
using AuthService.Infrastructure.EventBus;

namespace AuthService.Features.RegisterUser
{
    public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    DateTime RegisteredAt
) : IEvent;
}
