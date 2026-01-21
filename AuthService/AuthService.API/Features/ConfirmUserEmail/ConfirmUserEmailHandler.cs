using AuthService.API.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthService.API.Features.ConfirmUserEmail
{
    using MediatR;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ConfirmUserEmailHandler : IRequestHandler<ConfirmUserEmailCommand, string>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _httpContext;

        public ConfirmUserEmailHandler(UserManager<AppUser> userManager, IHttpContextAccessor httpContext)
        {
            _userManager = userManager;
            _httpContext = httpContext;
        }

        public async Task<string> Handle(ConfirmUserEmailCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);

            if (user is null)
                return "Invalid email confirmation request";

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);

            if (!result.Succeeded)
                return "Email confirmation failed";

            return "Email confirmed successfully";
        }
    }


}
