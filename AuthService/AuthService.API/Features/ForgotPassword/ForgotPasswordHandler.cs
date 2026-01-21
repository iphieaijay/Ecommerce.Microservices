using AuthService.API.Domain;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Shared.Contracts.Responses;

namespace AuthService.API.Features.ForgotPassword
{
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IHttpContextAccessor _httpContext;

        public ForgotPasswordHandler(
            UserManager<AppUser> userManager,
            IEmailSender emailSender,
            IHttpContextAccessor httpContext)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _httpContext = httpContext;
        }

        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
                return "If the email exists, a reset link has been sent";

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = $"https://l/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            await _emailSender.SendAsync(user.Email!, "Reset your password", resetLink);

            return "Password reset link sent";
        }
    }


}
