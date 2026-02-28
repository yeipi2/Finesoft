using fs_backend.Models;

namespace fs_backend.Repositories;

public interface IInvoiceStatusService
{
    void ApplyAfterPayment(Invoice invoice, decimal newBalance);
}
