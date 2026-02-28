using fs_backend.DTO;
using fs_backend.Models;

namespace fs_backend.Repositories;

public interface IInvoiceMapper
{
    Task<InvoiceDetailDto> MapToDetailDtoAsync(Invoice invoice);
}
