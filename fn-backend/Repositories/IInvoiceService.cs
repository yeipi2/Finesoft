using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceDetailDto>> GetInvoicesAsync(string? status = null, string? invoiceType = null, int? clientId = null);
    Task<InvoiceDetailDto?> GetInvoiceByIdAsync(int id);
    Task<ServiceResult<InvoiceDetailDto>> CreateInvoiceAsync(InvoiceDto invoiceDto, string createdByUserId);
    Task<ServiceResult<InvoiceDetailDto>> CreateInvoiceFromQuoteAsync(CreateInvoiceFromQuoteDto dto, string createdByUserId);
    Task<ServiceResult<InvoiceDetailDto>> UpdateInvoiceAsync(int id, InvoiceDto invoiceDto);
    Task<ServiceResult<bool>> DeleteInvoiceAsync(int id);
    Task<ServiceResult<bool>> ChangeInvoiceStatusAsync(int id, string newStatus, string? reason);
    Task<ServiceResult<InvoicePaymentDto>> AddPaymentAsync(int invoiceId, RegisterInvoicePaymentDto dto, string userId);
    Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(string userId);
    Task<InvoiceStatsDto> GetInvoiceStatsAsync();
    Task<byte[]> GenerateInvoicePdfAsync(int id);
    Task<List<int>> GetTicketsInUseAsync();
    Task<ServiceResult<InvoicePaymentDto>> AddPaymentWithReceiptAsync(
    int invoiceId,
    AddInvoicePaymentWithReceiptRequest request,
    string userId);
}