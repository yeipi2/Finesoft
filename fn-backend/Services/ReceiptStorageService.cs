using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.AspNetCore.Http;

namespace fs_backend.Services;

public class ReceiptStorageService : IReceiptStorageService
{
    private readonly IWebHostEnvironment _environment;

    public ReceiptStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)?>> SaveOptionalAsync(
        int invoiceId,
        IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)?>.Success(null);

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(file.ContentType))
        {
            return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)?>.Failure(
                "Tipo de archivo no permitido (solo PDF/JPG/PNG)");
        }

        var folderRelative = Path.Combine("uploads", "invoices", invoiceId.ToString(), "payments");
        var folderPhysical = Path.Combine(_environment.WebRootPath, folderRelative);
        Directory.CreateDirectory(folderPhysical);

        var ext = Path.GetExtension(file.FileName);
        var safeExt = string.IsNullOrWhiteSpace(ext) ? ".bin" : ext.ToLowerInvariant();
        var storedFileName = $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{safeExt}";
        var filePhysicalPath = Path.Combine(folderPhysical, storedFileName);

        await using (var stream = new FileStream(filePhysicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var uploadedAt = DateTime.UtcNow;
        var relativePath = Path.Combine(folderRelative, storedFileName).Replace("\\", "/");
        return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)?>.Success(
            (relativePath, file.FileName, file.ContentType, file.Length, uploadedAt));
    }

    public async Task<ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)>> SaveRequiredAsync(
        int invoiceId,
        IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)>.Failure(
                "Debes subir un comprobante");
        }

        var allowed = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || file.ContentType == "application/pdf";
        if (!allowed)
            return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)>.Failure("Solo se permite PDF o imagen");

        if (file.Length > 10 * 1024 * 1024)
            return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)>.Failure("El archivo excede 10MB");

        var folder = Path.Combine(_environment.WebRootPath, "receipts", "invoices", invoiceId.ToString());
        Directory.CreateDirectory(folder);

        var safeExt = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(safeExt))
            safeExt = ".bin";

        var storedName = $"{Guid.NewGuid():N}{safeExt}";
        var storedPath = Path.Combine(folder, storedName);

        await using (var stream = File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        var uploadedAt = DateTime.UtcNow;
        var relativePath = $"/receipts/invoices/{invoiceId}/{storedName}";
        return ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)>.Success(
            (relativePath, file.FileName, file.ContentType, file.Length, uploadedAt));
    }
}
