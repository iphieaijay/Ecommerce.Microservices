using AuthService.Features.LoginUser;

namespace AuthService.Features.LoginUser
{
    public record LoginUserResponse(
        string AccessToken,string RefreshToken,
    DateTime ExpiresAt, UserInfo User);

   
}
