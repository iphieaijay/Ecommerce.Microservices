using AuthService.Features.LogoutUser;
using FluentValidation;

namespace AuthService.Features.LogoutUser
{
    public class LogoutUserValidator : AbstractValidator<LogoutUserCommand>
    {
        public LogoutUserValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("Access token is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}
