using FluentValidation;

namespace ProductService.Feature.UpdateProduct
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name must not exceed 200 characters");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative")
                .LessThan(1000000).WithMessage("Price exceeds maximum allowed value");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative")
                .LessThan(1000000).WithMessage("Stock quantity exceeds maximum allowed value");

            RuleFor(x => x.UpdatedBy)
                .NotEmpty().WithMessage("UpdatedBy is required")
                .MaximumLength(100).WithMessage("UpdatedBy must not exceed 100 characters");
        }
    }

}
