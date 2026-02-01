using InvoiceService.Domain.Entities;
using InvoiceService.Infrastructure.Persistence.Repositories;

namespace InvoiceService.Infrastructure.BackgroundServices;

public class RetryFailedInvoicesService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RetryFailedInvoicesService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private const int MaxRetryCount = 3;

    public RetryFailedInvoicesService(IServiceProvider serviceProvider, ILogger<RetryFailedInvoicesService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RetryFailedInvoicesService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFailedInvoices(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RetryFailedInvoicesService");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("RetryFailedInvoicesService stopped");
    }

    private async Task ProcessFailedInvoices(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

        var failedInvoices = await repository.GetFailedInvoicesForRetryAsync(MaxRetryCount, cancellationToken);

        if (failedInvoices.Count == 0)
            return;

        _logger.LogInformation("Found {Count} failed invoices to retry", failedInvoices.Count);

        foreach (var invoice in failedInvoices)
        {
            try
            {
                // Attempt to reprocess
                invoice.MarkAsIssued();
                invoice.MarkAsPaid();

                await repository.UpdateAsync(invoice, cancellationToken);

                _logger.LogInformation(
                    "Successfully retried invoice {InvoiceNumber} (retry #{RetryCount})",
                    invoice.InvoiceNumber, invoice.RetryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to retry invoice {InvoiceNumber} (retry #{RetryCount})",
                    invoice.InvoiceNumber, invoice.RetryCount);

                invoice.MarkAsFailed($"Retry failed: {ex.Message}");
                await repository.UpdateAsync(invoice, cancellationToken);
            }
        }
    }
}
