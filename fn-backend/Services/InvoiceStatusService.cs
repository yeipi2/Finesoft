using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;

namespace fs_backend.Services;

public class InvoiceStatusService : IInvoiceStatusService
{
    public void ApplyAfterPayment(Invoice invoice, decimal newBalance)
    {
        if (newBalance <= 0 || IsZero(newBalance))
        {
            invoice.Status = InvoiceConstants.Status.Paid;
            invoice.PaidDate ??= DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceConstants.Status.Pending;
            invoice.PaidDate = null;
        }
    }

    private static bool IsZero(decimal value)
        => Math.Abs(value) < 0.01m;
}
