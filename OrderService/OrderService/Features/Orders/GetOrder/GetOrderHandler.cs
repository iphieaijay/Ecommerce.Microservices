namespace OrderService.Features.Orders.GetOrder
{
   
    using global::OrderService.Infrastructure.Persistence;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using OrderService.Infrastructure.Persistence;

    public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

    public record OrderDto(
        Guid Id,
        string UserId,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        string? CancellationReason,
        List<OrderItemDetailDto> Items
    );

    public record OrderItemDetailDto(
        Guid Id,
        string ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );

    public class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto?>
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<GetOrderHandler> _logger;

        public GetOrderHandler(OrderDbContext context, ILogger<GetOrderHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                    return null;
                }

                return new OrderDto(
                    order.Id,
                    order.UserId,
                    order.Status.ToString(),
                    order.TotalAmount,
                    order.CreatedAt,
                    order.UpdatedAt,
                    order.CancellationReason,
                    order.Items.Select(i => new OrderItemDetailDto(
                        i.Id,
                        i.ProductId,
                        i.ProductName,
                        i.Quantity,
                        i.UnitPrice,
                        i.TotalPrice
                    )).ToList()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", request.OrderId);
                throw;
            }
        }
    }
}
