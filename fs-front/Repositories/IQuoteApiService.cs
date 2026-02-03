using fs_front.DTO;

namespace fs_front.Services;

public interface IQuoteApiService
{
    Task<List<QuoteDetailDto>?> GetQuotesAsync(string? status = null, int? clientId = null);
    Task<QuoteDetailDto?> GetQuoteByIdAsync(int id);
    Task<(bool Success, QuoteDetailDto? CreatedQuote, string? ErrorMessage)> CreateQuoteAsync(QuoteDto quote);
    Task<(bool Success, string? ErrorMessage)> UpdateQuoteAsync(int id, QuoteDto quote);
    Task<(bool Success, string? ErrorMessage)> DeleteQuoteAsync(int id);
    Task<(bool Success, string? ErrorMessage)> ChangeQuoteStatusAsync(int id, string newStatus);
    Task<byte[]?> GenerateQuotePdfAsync(int id);
}