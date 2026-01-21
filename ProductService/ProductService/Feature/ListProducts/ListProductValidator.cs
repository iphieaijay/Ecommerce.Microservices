using FluentValidation;
using ProductService.Feature.ListProducts;

namespace ProductService.Feature.ListProducts
{
   
    public class ListProductValidator : AbstractValidator<ListProductQuery>
    {
        public ListProductValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100");

            RuleFor(x => x.SortBy)
                .Must(x => new[] { "Name", "Price", "StockQuantity", "CreatedAt", "Category" }.Contains(x))
                .WithMessage("Invalid sort field");
        }
    }

}
