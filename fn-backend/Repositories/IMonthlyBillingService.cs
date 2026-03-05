// fs-backend/Repositories/IMonthlyBillingService.cs
using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface IMonthlyBillingService
{
    // ⭐ ACTUALIZADO: recibe lista de items con PaymentMethod + PaymentForm por cliente
    Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(string userId, List<GenerateMonthlyInvoiceItemDto> items);
    Task<List<MonthlyClientSummaryDto>> GetMonthlySummaryAsync();
}