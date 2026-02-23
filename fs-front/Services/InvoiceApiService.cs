using System.Net.Http.Json;
using fs_front.DTO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;


namespace fs_front.Services;

public class InvoiceApiService : IInvoiceApiService
{
    private readonly HttpClient _httpClient;

    public InvoiceApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InvoiceDetailDto>?> GetInvoicesAsync(string? status = null, string? invoiceType = null,
        int? clientId = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            if (!string.IsNullOrEmpty(invoiceType)) queryParams.Add($"invoiceType={invoiceType}");
            if (clientId.HasValue) queryParams.Add($"clientId={clientId}");

            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            return await _httpClient.GetFromJsonAsync<List<InvoiceDetailDto>>($"api/invoices{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener facturas: {e.Message}");
            return null;
        }
    }

    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<InvoiceDetailDto>($"api/invoices/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener factura {id}: {e.Message}");
            return null;
        }
    }
    public async Task<(bool Success, InvoiceDetailDto? CreatedInvoice, string? ErrorMessage)> CreateInvoiceAsync(InvoiceDto invoice)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/invoices", invoice);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<InvoiceDetailDto>();
                return (true, created, null);
            }

            // ⭐ MEJORADO: extraer mensajes legibles
            var errorContent = await response.Content.ReadAsStringAsync();
            var friendlyMessage = ExtractErrorMessage(errorContent);
            return (false, null, friendlyMessage);
        }
        catch (Exception e)
        {
            return (false, null, $"Error de conexión: {e.Message}");
        }
    }

    // ⭐ AGREGAR este método helper en la misma clase:
    private string ExtractErrorMessage(string errorContent)
    {
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(errorContent);

            // Formato ASP.NET validation errors: {"errors": {"Field": ["mensaje"]}}
            if (json.RootElement.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                foreach (var field in errors.EnumerateObject())
                {
                    foreach (var msg in field.Value.EnumerateArray())
                    {
                        messages.Add(msg.GetString() ?? "");
                    }
                }
                if (messages.Any())
                    return string.Join(" | ", messages);
            }

            // Formato simple: {"message": "..."}
            if (json.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? errorContent;
        }
        catch { }

        return errorContent;
    }

    public async Task<(bool Success, InvoiceDetailDto? CreatedInvoice, string? ErrorMessage)>
        CreateInvoiceFromQuoteAsync(CreateInvoiceFromQuoteDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/invoices/from-quote", dto);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<InvoiceDetailDto>();
                return (true, created, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            var friendlyMessage = ExtractErrorMessage(errorContent);
            return (false, null, friendlyMessage);
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateInvoiceAsync(int id, InvoiceDto invoice)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/invoices/{id}", invoice);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar factura: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteInvoiceAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/invoices/{id}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar factura: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangeInvoiceStatusAsync(int id, string newStatus, string? reason = null)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/invoices/{id}/status",
                new { Status = newStatus, Reason = reason });

            if (response.IsSuccessStatusCode)
                return (true, null);

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al cambiar estado: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }


    public async Task<(bool Success, InvoicePaymentDto? AddedPayment, string? ErrorMessage)> AddPaymentAsync(
        int invoiceId, InvoicePaymentDto payment)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/invoices/{invoiceId}/payments", payment);
            if (response.IsSuccessStatusCode)
            {
                var added = await response.Content.ReadFromJsonAsync<InvoicePaymentDto>();
                return (true, added, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al registrar pago: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, InvoicePaymentDto? AddedPayment, string? ErrorMessage)>
    AddPaymentWithReceiptAsync(int invoiceId, InvoicePaymentDto payment, IBrowserFile receiptFile)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            // Campos (ojo: nombres deben coincidir con el DTO del backend)
            content.Add(new StringContent(payment.Amount.ToString(CultureInfo.InvariantCulture)), "Amount");
            content.Add(new StringContent(payment.PaymentDate.ToString("O")), "PaymentDate");
            content.Add(new StringContent(payment.PaymentMethod ?? ""), "PaymentMethod");
            content.Add(new StringContent(payment.Reference ?? ""), "Reference");
            content.Add(new StringContent(payment.Notes ?? ""), "Notes");

            // Archivo: debe llamarse EXACTO "Receipt"
            var stream = receiptFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(receiptFile.ContentType);

            content.Add(fileContent, "Receipt", receiptFile.Name);

            var response = await _httpClient.PostAsync(
                $"api/invoices/{invoiceId}/payments-with-receipt",
                content
            );

            if (response.IsSuccessStatusCode)
            {
                var added = await response.Content.ReadFromJsonAsync<InvoicePaymentDto>();
                return (true, added, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, errorContent);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> GenerateMonthlyInvoicesAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/invoices/generate-monthly", null);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al generar facturas: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<InvoiceStatsDto?> GetInvoiceStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<InvoiceStatsDto>("api/invoices/stats");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener estadísticas: {e.Message}");
            return null;
        }
    }

    public async Task<List<int>?> GetTicketsInUseAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<int>>("api/invoices/tickets-in-use");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tickets en uso: {e.Message}");
            return null;
        }
    }

    public async Task<byte[]?> GenerateInvoicePdfAsync(int id)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync($"api/invoices/{id}/pdf");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al generar PDF: {e.Message}");
            return null;
        }
    }
}
