namespace OrderService.Features.Orders.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<CancelOrderResult>;

public record CancelOrderResult(Guid OrderId, string Status);

public class CancelOrderValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, CancelOrderResult>
{
    private readonly OrderDbContext _context;
    private readonly ILogger<CancelOrderHandler> _logger;

    public CancelOrderHandler(OrderDbContext context, ILogger<CancelOrderHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CancelOrderResult> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

            if (order == null)
                throw new InvalidOperationException($"Order {request.OrderId} not found");

            order.Cancel(request.Reason);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} cancelled. Reason: {Reason}",
                order.Id, request.Reason);

            return new CancelOrderResult(order.Id, order.Status.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", request.OrderId);
            throw;
        }
    }
}
