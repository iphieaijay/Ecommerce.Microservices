using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Application.Common.Behaviors
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

        public TransactionBehavior(
            ProductDbContext context,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only use transactions for commands (not queries)
            if (typeof(TRequest).Name.EndsWith("Query"))
            {
                return await next();
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var response = await next();
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Transaction committed for {RequestName}",
                        typeof(TRequest).Name);

                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    _logger.LogError(
                        ex,
                        "Transaction rolled back for {RequestName}",
                        typeof(TRequest).Name);

                    throw;
                }
            });
        }
    }
}

