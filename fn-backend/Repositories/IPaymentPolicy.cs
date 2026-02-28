using fs_backend.Models;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface IPaymentPolicy
{
    bool TryNormalizePayType(string? value, out string normalizedPayType);
    string NormalizePayType(string? value);
    string NormalizePaymentMethodOrThrow(string? value);
    ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)> ValidateInvoicePayment(
        Invoice invoice,
        decimal amount,
        string? paymentMethod);
}
