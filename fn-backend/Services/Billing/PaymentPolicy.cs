using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;

namespace fs_backend.Services;

public class PaymentPolicy : IPaymentPolicy
{
    private static readonly Dictionary<string, string> PaymentMethodMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["transferencia"] = "Transferencia",
            ["efectivo"] = "Efectivo",
            ["tarjeta"] = "Tarjeta",
            ["cheque"] = "Cheque",
            ["deposito"] = "Depósito",
            ["depósito"] = "Depósito",
            ["Transferencia"] = "Transferencia",
            ["Efectivo"] = "Efectivo",
            ["Tarjeta"] = "Tarjeta",
            ["Cheque"] = "Cheque",
            ["Depósito"] = "Depósito",
            ["Deposito"] = "Depósito"
        };

    public bool TryNormalizePayType(string? value, out string normalizedPayType)
    {
        var candidate = (value ?? InvoiceConstants.PaymentType.Ppd).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = InvoiceConstants.PaymentType.Ppd;

        if (candidate is InvoiceConstants.PaymentType.Pue or InvoiceConstants.PaymentType.Ppd)
        {
            normalizedPayType = candidate;
            return true;
        }

        normalizedPayType = string.Empty;
        return false;
    }

    public string NormalizePayType(string? value)
    {
        return TryNormalizePayType(value, out var normalized) ? normalized : InvoiceConstants.PaymentType.Ppd;
    }

    public string NormalizePaymentMethodOrThrow(string? value)
    {
        var raw = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        raw = raw.TrimEnd('.');

        if (PaymentMethodMap.TryGetValue(raw, out var canonical))
            return canonical;

        throw new ArgumentException("PaymentMethod inválido. Usa: Transferencia, Efectivo, Tarjeta, Cheque o Depósito.");
    }

    public ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)> ValidateInvoicePayment(
        Invoice invoice,
        decimal amount,
        string? paymentMethod)
    {
        var total = Round2(invoice.Total);
        var alreadyPaid = Round2(invoice.Payments.Sum(p => p.Amount));
        var balance = Round2(total - alreadyPaid);

        if (balance <= 0)
            return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure("Esta factura ya está saldada");

        var normalizedAmount = Round2(amount);
        if (normalizedAmount <= 0)
            return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure("El monto debe ser mayor a 0");

        var method = paymentMethod;
        var paymentType = NormalizePayType(invoice.PaymentType);

        if (paymentType == InvoiceConstants.PaymentType.Pue)
        {
            if (!IsZero(balance - normalizedAmount))
                return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure($"Para PUE el monto debe ser exactamente {balance:N2}");

            if (!string.IsNullOrWhiteSpace(invoice.PaymentMethod))
                method = invoice.PaymentMethod;

            if (string.IsNullOrWhiteSpace(method))
            {
                return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure(
                    "Para PUE debes tener método de pago definido (en factura o en el pago)");
            }
        }
        else
        {
            if (normalizedAmount > balance)
            {
                return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure(
                    $"No puedes registrar un pago mayor al saldo pendiente ({balance:N2})");
            }

            if (string.IsNullOrWhiteSpace(method))
                return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure("El método de pago es obligatorio");
        }

        try
        {
            var normalizedMethod = NormalizePaymentMethodOrThrow(method);
            return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Success(
                (normalizedAmount, normalizedMethod, balance));
        }
        catch (ArgumentException ex)
        {
            return ServiceResult<(decimal Amount, string PaymentMethod, decimal BalanceBefore)>.Failure(ex.Message);
        }
    }

    private static decimal Round2(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static bool IsZero(decimal value)
        => Math.Abs(value) < 0.01m;
}
