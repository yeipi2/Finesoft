using fs_backend.Util;
using Microsoft.AspNetCore.Http;

namespace fs_backend.Repositories;

public interface IReceiptStorageService
{
    Task<ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)?>> SaveOptionalAsync(
        int invoiceId,
        IFormFile? file);

    Task<ServiceResult<(string Path, string FileName, string ContentType, long Size, DateTime UploadedAt)>> SaveRequiredAsync(
        int invoiceId,
        IFormFile? file);
}
