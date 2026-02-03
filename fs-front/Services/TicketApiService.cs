using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class TicketApiService : ITicketApiService
{
    private readonly HttpClient _httpClient;

    public TicketApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TicketDetailDto>?> GetTicketsAsync(string? status = null, string? priority = null,
        int? serviceId = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            if (!string.IsNullOrEmpty(priority)) queryParams.Add($"priority={priority}");
            if (serviceId.HasValue) queryParams.Add($"serviceId={serviceId}");

            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            return await _httpClient.GetFromJsonAsync<List<TicketDetailDto>>($"api/tickets{query}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener tickets: {e.Message}");
            return null;
        }
    }

    public async Task<TicketDetailDto?> GetTicketByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TicketDetailDto>($"api/tickets/{id}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener ticket {id}: {e.Message}");
            return null;
        }
    }

    public async Task<TicketStatsDto?> GetTicketStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TicketStatsDto>("api/tickets/stats");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error al obtener estadísticas: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, TicketDetailDto? CreatedTicket, string? ErrorMessage)> CreateTicketAsync(
        TicketDto ticket)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/tickets", ticket);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<TicketDetailDto>();
                return (true, created, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al crear ticket: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateTicketAsync(int id, TicketDto ticket)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/tickets/{id}", ticket);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar ticket: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteTicketAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/tickets/{id}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar ticket: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, TicketCommentDto? AddedComment, string? ErrorMessage)> AddCommentAsync(
        int ticketId, TicketCommentDto comment)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/comments", comment);
            if (response.IsSuccessStatusCode)
            {
                var added = await response.Content.ReadFromJsonAsync<TicketCommentDto>();
                return (true, added, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al agregar comentario: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }
}