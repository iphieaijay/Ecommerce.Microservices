using MediatR;
using OrderService.Infrastructure.Persistence;
using OrderService.Features.Orders.GetOrder;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Features.Orders.GetUserOrders;


public record GetUserOrdersQuery(string UserId, int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedOrdersResult>;

public record PaginatedOrdersResult(
    List<OrderDto> Orders,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public class GetUserOrdersHandler : IRequestHandler<GetUserOrdersQuery, PaginatedOrdersResult>
{
    private readonly OrderDbContext _context;
    private readonly ILogger<GetUserOrdersHandler> _logger;

    public GetUserOrdersHandler(OrderDbContext context, ILogger<GetUserOrdersHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaginatedOrdersResult> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == request.UserId)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrderDto(
                    o.Id,
                    o.UserId,
                    o.Status.ToString(),
                    o.TotalAmount,
                    o.CreatedAt,
                    o.UpdatedAt,
                    o.CancellationReason,
                    o.Items.Select(i => new OrderItemDetailDto(
                        i.Id,
                        i.ProductId,
                        i.ProductName,
                        i.Quantity,
                        i.UnitPrice,
                        i.TotalPrice
                    )).ToList()
                ))
                .ToListAsync(cancellationToken);

            return new PaginatedOrdersResult(orders, totalCount, request.PageNumber, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user {UserId}", request.UserId);
            throw;
        }
    }
}
