using FluentValidation;

namespace AuthService.Features.LoginUser
{
    public class LoginUserValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserValidator()
        {
            RuleFor(x => x.EmailOrUsername)
                .NotEmpty().WithMessage("Email or username is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}
