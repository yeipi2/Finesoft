using fs_front.DTO;
using Microsoft.AspNetCore.Components.Forms;

namespace fs_front.Services;

public interface IInvoiceApiService
{
    Task<List<InvoiceDetailDto>?> GetInvoicesAsync(string? status = null, string? invoiceType = null, int? clientId = null);
    Task<InvoiceDetailDto?> GetInvoiceByIdAsync(int id);
    Task<(bool Success, InvoiceDetailDto? CreatedInvoice, string? ErrorMessage)> CreateInvoiceAsync(InvoiceDto invoice);
    Task<(bool Success, InvoiceDetailDto? CreatedInvoice, string? ErrorMessage)> CreateInvoiceFromQuoteAsync(CreateInvoiceFromQuoteDto dto);
    Task<(bool Success, string? ErrorMessage)> UpdateInvoiceAsync(int id, InvoiceDto invoice);
    Task<(bool Success, string? ErrorMessage)> DeleteInvoiceAsync(int id);
    Task<(bool Success, string? ErrorMessage)> ChangeInvoiceStatusAsync(int id, string newStatus, string? reason = null);
    Task<(bool Success, InvoicePaymentDto? AddedPayment, string? ErrorMessage)> AddPaymentAsync(int invoiceId, InvoicePaymentDto payment);
    Task<(bool Success, string? ErrorMessage)> GenerateMonthlyInvoicesAsync();
    Task<InvoiceStatsDto?> GetInvoiceStatsAsync();
    Task<byte[]?> GenerateInvoicePdfAsync(int id);
<<<<<<< HEAD
    Task<List<int>?> GetTicketsInUseAsync();
=======
    Task<(bool Success, InvoicePaymentDto? AddedPayment, string? ErrorMessage)> AddPaymentWithReceiptAsync(
        int invoiceId,
        InvoicePaymentDto payment,
        IBrowserFile receiptFile
    );


>>>>>>> 9d28ed7 (feat(invoices): implement payments module with receipt upload, persistence and download link)
}