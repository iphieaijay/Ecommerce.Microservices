namespace AuthService.Features.RefreshToken
{
    public record RefreshTokenResponse(string AccessToken,string RefreshToken,DateTime ExpiresAt);
}
