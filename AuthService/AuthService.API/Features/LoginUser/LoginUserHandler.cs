using AuthService.API.Common.JWT;
using AuthService.API.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthService.API.Features.LoginUser;

public class LoginUserHandler : IRequestHandler<LoginUserCommand, AuthResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public LoginUserHandler(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) throw new Exception("Invalid credentials");
        if (!await _userManager.IsEmailConfirmedAsync(user)) throw new Exception("Email not confirmed");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded) throw new Exception("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = await _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshToken(user);

        return new AuthResponse { AccessToken = accessToken, RefreshToken = refreshToken.Token };
    }
}
