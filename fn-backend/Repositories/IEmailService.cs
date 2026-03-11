namespace fs_backend.Services;

public interface IEmailService
{
    Task<bool> SendQuoteEmailAsync(string toEmail, string clientName, string quoteNumber, byte[] pdfBytes, string publicToken);
    Task<bool> SendReportEmailAsync(string toEmail, string userName, byte[] pdfBytes, DateTime startDate, DateTime endDate);
}