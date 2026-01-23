using MediatR;
namespace AuthService.Features.ResetPassword
{
   
    public record ResetPasswordCommand(string Email, string Token,string NewPassword,string ConfirmPassword) : IRequest<ResetPasswordResponse>;


}
