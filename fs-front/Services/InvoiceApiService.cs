using System.Net.Http.Json;
using fs_front.DTO;

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

    public async Task<(bool Success, InvoiceDetailDto? CreatedInvoice, string? ErrorMessage)> CreateInvoiceAsync(
        InvoiceDto invoice)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/invoices", invoice);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<InvoiceDetailDto>();
                return (true, created, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear factura: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
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
            return (false, null, $"Error al crear factura: {errorContent}");
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

    public async Task<(bool Success, string? ErrorMessage)> ChangeInvoiceStatusAsync(int id, string newStatus)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/invoices/{id}/status", new { Status = newStatus });
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

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