using FluentValidation;

namespace ProductService.Feature.GetProduct
{
    public class CreateProductValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name must not exceed 200 characters")
                .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Product name contains invalid characters");

            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU is required")
                .MaximumLength(50).WithMessage("SKU must not exceed 50 characters")
                .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("SKU must contain only alphanumeric characters and hyphens");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative")
                .LessThan(2000000).WithMessage("Price exceeds maximum allowed value");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be non-negative")
                .LessThan(1000000).WithMessage("Stock quantity exceeds maximum allowed value");

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Category must not exceed 100 characters");

            RuleFor(x => x.CreatedBy)
                .NotEmpty().WithMessage("CreatedBy is required")
                .MaximumLength(100).WithMessage("CreatedBy must not exceed 100 characters");
        }
    }

}
