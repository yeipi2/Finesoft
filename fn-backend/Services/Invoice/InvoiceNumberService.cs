using fs_backend.Identity;
using fs_backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly ApplicationDbContext _context;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public InvoiceNumberService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(int maxRetries = 5)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            await _semaphore.WaitAsync();
            try
            {
                var year = DateTime.UtcNow.Year;
                var prefix = $"INV-{year}-";

                var strategy = _context.Database.CreateExecutionStrategy();
                
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var lastInvoice = await _context.Invoices
                            .Where(i => i.InvoiceNumber.StartsWith(prefix))
                            .OrderByDescending(i => i.Id)
                            .FirstOrDefaultAsync();

                        var nextNumber = 1;
                        if (lastInvoice != null)
                        {
                            var parts = lastInvoice.InvoiceNumber.Split('-');
                            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
                                nextNumber = lastNumber + 1;
                        }

                        var invoiceNumber = $"{prefix}{nextNumber:D4}";

                        var exists = await _context.Invoices
                            .AnyAsync(i => i.InvoiceNumber == invoiceNumber);

                        if (exists)
                        {
                            await transaction.RollbackAsync();
                            throw new InvalidOperationException($"Invoice number {invoiceNumber} already exists");
                        }

                        await transaction.CommitAsync();
                        return invoiceNumber;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        throw new InvalidOperationException("No se pudo generar un número de factura único después de múltiples intentos");
    }
}
