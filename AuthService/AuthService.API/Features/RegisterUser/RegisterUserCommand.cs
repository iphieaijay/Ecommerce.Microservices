using MediatR;

namespace AuthService.API.Features.RegisterUser;

public record RegisterUserCommand(string Email, string FirstName, 
    string LastName, string Password,string PhoneNo, string Address, 
    string Role) : IRequest<string>;

