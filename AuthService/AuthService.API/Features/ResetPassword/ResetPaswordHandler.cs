using AuthService.API.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Shared.Contracts.Responses;

namespace AuthService.API.Features.ResetPassword
{
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, string>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _httpContext;

        public ResetPasswordHandler(UserManager<AppUser> userManager, IHttpContextAccessor httpContext)
        {
            _userManager = userManager;
            _httpContext = httpContext;
        }

        public async Task<string> Handle(ResetPasswordCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user is null)
                return "Invalid password reset request";

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
                return "Password reset failed";

            return "Password reset successfully";
        }
    }

}
