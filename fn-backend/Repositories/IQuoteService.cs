using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface IQuoteService
{
    Task<IEnumerable<QuoteDetailDto>> GetQuotesAsync(string? status = null, int? clientId = null);
    Task<QuoteDetailDto?> GetQuoteByIdAsync(int id);
    Task<ServiceResult<QuoteDetailDto>> CreateQuoteAsync(QuoteDto quoteDto, string createdByUserId);
    Task<ServiceResult<QuoteDetailDto>> UpdateQuoteAsync(int id, QuoteDto quoteDto);
    Task<ServiceResult<bool>> DeleteQuoteAsync(int id);
    Task<ServiceResult<bool>> ChangeQuoteStatusAsync(int id, string newStatus);
    Task<byte[]> GenerateQuotePdfAsync(int id);
    Task<QuoteDetailDto?> GetQuoteByPublicTokenAsync(string token);
    Task<ServiceResult<bool>> RespondToQuoteAsync(string token, string status, string? comments);
    Task<ServiceResult<bool>> SendQuoteEmailAsync(int quoteId);
}