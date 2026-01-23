namespace AuthService.Features.LoginUser
{
    public record UserInfo(
    Guid Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    IList<string> Roles
);
}
