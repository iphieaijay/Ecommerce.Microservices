using InvoiceService.Domain.Entities;

namespace InvoiceService.Infrastructure.Persistence.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default);
    Task<List<Invoice>> GetFailedInvoicesForRetryAsync(int maxRetryCount, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
