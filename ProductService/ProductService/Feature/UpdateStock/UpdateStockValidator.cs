using FluentValidation;

namespace ProductService.Feature.UpdateStock
{
   
    public class UpdateStockValidator : AbstractValidator<UpdateStockCommand>
    {
        public UpdateStockValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.QuantityChange)
                .NotEqual(0).WithMessage("Quantity change cannot be zero")
                .GreaterThan(-1000000).WithMessage("Quantity change is too large (negative)")
                .LessThan(1000000).WithMessage("Quantity change is too large (positive)");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

            RuleFor(x => x.UpdatedBy)
                .NotEmpty().WithMessage("UpdatedBy is required");
        }
    }

}
