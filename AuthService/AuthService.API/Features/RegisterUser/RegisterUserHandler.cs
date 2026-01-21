using AuthService.API.Common.JWT;
using AuthService.API.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace AuthService.API.Features.RegisterUser;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, string>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;

    public RegisterUserHandler(UserManager<AppUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var user = new AppUser { UserName = request.Email, FirstName=request.FirstName, LastName=request.LastName, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) throw new Exception(string.Join(",", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);

        // Send email confirmation link here using token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = $"https://yourfrontend/confirmemail?userId={user.Id}&token={WebUtility.UrlEncode(token)}";
        return confirmationLink; 
    }
}

