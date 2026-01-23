using MediatR;

namespace AuthService.Features.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string UserName,
    string Password,
    string ConfirmPassword,
    string FirstName,
    string LastName,string PhoneNo, string Address
) : IRequest<RegisterUserResponse>;


