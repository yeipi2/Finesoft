namespace fs_backend.Repositories;

public interface IInvoiceNumberService
{
    Task<string> GenerateAsync(int maxRetries = 5);
}
