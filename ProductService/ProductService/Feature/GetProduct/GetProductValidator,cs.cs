using FluentValidation;

namespace ProductService.Feature.GetProduct
{
    // Validator
    public class GetProductValidator : AbstractValidator<GetProductQuery>
    {
        public GetProductValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required");
        }
    }

}
