using FluentValidation;

namespace ProductService.Feature.DeleteProduct
{


    // Validator
    public class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
    {
        public DeleteProductValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.DeletedBy)
                .NotEmpty().WithMessage("DeletedBy is required");
        }
    }

}
