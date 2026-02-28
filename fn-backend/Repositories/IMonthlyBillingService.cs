using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface IMonthlyBillingService
{
    Task<ServiceResult<bool>> GenerateMonthlyInvoicesAsync(string userId, List<int>? clientIds = null);
    Task<List<MonthlyClientSummaryDto>> GetMonthlySummaryAsync();
}
