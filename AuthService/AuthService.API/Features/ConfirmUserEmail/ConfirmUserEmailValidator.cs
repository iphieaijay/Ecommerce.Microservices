using AuthService.Features.ConfirmUserEmail;
using FluentValidation;

namespace AuthService.Features.ConfirmUserEmail
{
    public class ConfirmUserEmailValidator : AbstractValidator<ConfirmUserEmailCommand>
    {
        public ConfirmUserEmailValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Confirmation token is required");
        }
    }

}
