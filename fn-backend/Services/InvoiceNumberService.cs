using fs_backend.Identity;
using fs_backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class InvoiceNumberService : IInvoiceNumberService
{
    private readonly ApplicationDbContext _context;

    public InvoiceNumberService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync()
    {
        var year = DateTime.UtcNow.Year;
        var lastInvoice = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}"))
            .OrderByDescending(i => i.Id)
            .FirstOrDefaultAsync();

        var nextNumber = 1;
        if (lastInvoice != null)
        {
            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"INV-{year}-{nextNumber:D4}";
    }
}
