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
        int? serviceId = null, string? userId = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            if (!string.IsNullOrEmpty(priority)) queryParams.Add($"priority={priority}");
            if (serviceId.HasValue) queryParams.Add($"serviceId={serviceId}");
            if (!string.IsNullOrEmpty(userId)) queryParams.Add($"userId={userId}");

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

    public async Task<TicketStatsDto?> GetTicketStatsAsync(string? userId = null)
    {
        try
        {
            var url = "api/tickets/stats";
            if (!string.IsNullOrEmpty(userId))
            {
                url += $"?userId={userId}";
            }
            return await _httpClient.GetFromJsonAsync<TicketStatsDto>(url);
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

    // ========== MÉTODOS PARA ACTIVIDADES ==========

    public async Task<(bool Success, TicketActivityDto? AddedActivity, string? ErrorMessage)> AddActivityAsync(
        int ticketId, TicketActivityDto activity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/tickets/{ticketId}/activities", activity);
            if (response.IsSuccessStatusCode)
            {
                var added = await response.Content.ReadFromJsonAsync<TicketActivityDto>();
                return (true, added, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al agregar actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, TicketActivityDto? UpdatedActivity, string? ErrorMessage)> UpdateActivityAsync(
        int ticketId, int activityId, TicketActivityDto activity)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/tickets/{ticketId}/activities/{activityId}", activity);
            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<TicketActivityDto>();
                return (true, updated, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, null, $"Error al actualizar actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, null, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteActivityAsync(int ticketId, int activityId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/tickets/{ticketId}/activities/{activityId}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al eliminar actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CompleteActivityAsync(int ticketId, int activityId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/tickets/{ticketId}/activities/{activityId}/complete", null);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al completar actividad: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
    // ============================================================
    // ACTUALIZACIÓN 5: TicketApiService.cs (Frontend)
    // Agregar este método al final de la clase TicketApiService
    // ============================================================

    // 🆕 NUEVO MÉTODO - Agregar al final de la clase
    public async Task<(bool Success, string? ErrorMessage)> UpdateTicketStatusAsync(int ticketId, string newStatus)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"api/tickets/{ticketId}/status",
                new { Status = newStatus });

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"Error al actualizar estado: {errorContent}");
        }
        catch (Exception e)
        {
            return (false, $"Error: {e.Message}");
        }
    }
}