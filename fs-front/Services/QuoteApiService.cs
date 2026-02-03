using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class QuoteApiService : IQuoteApiService
{
    private readonly HttpClient _httpClient;

    public QuoteApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<QuoteDetailDto>?> GetQuotesAsync(string? status = null, int? clientId = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            if (clientId.HasValue) queryParams.Add($"clientId={clientId}");

            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            return await _httpClient.GetFromJsonAsync<List<QuoteDetailDto>>($"api/quotes{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener cotizaciones: {e.Message}");
            return null;
        }
    }

    public async Task<QuoteDetailDto?> GetQuoteByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<QuoteDetailDto>($"api/quotes/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener cotización {id}: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, QuoteDetailDto? CreatedQuote, string? ErrorMessage)> CreateQuoteAsync(
        QuoteDto quote)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/quotes", quote);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<QuoteDetailDto>();
                return (true, created, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear cotización: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateQuoteAsync(int id, QuoteDto quote)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/quotes/{id}", quote);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar cotización: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteQuoteAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/quotes/{id}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar cotización: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ChangeQuoteStatusAsync(int id, string newStatus)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/quotes/{id}/status", new { Status = newStatus });
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

    public async Task<byte[]?> GenerateQuotePdfAsync(int id)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync($"api/quotes/{id}/pdf");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al generar PDF: {e.Message}");
            return null;
        }
    }
}