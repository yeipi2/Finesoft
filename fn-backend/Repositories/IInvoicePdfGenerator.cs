using fs_backend.Models;

namespace fs_backend.Repositories;

public interface IInvoicePdfGenerator
{
    byte[] Generate(Invoice invoice, string createdBy);
}
