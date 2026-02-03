using fs_front.DTO;

namespace fs_front.Services;

public interface ITicketApiService
{
    Task<List<TicketDetailDto>?> GetTicketsAsync(string? status = null, string? priority = null, int? serviceId = null, string? userId = null);
    Task<TicketDetailDto?> GetTicketByIdAsync(int id);
    Task<TicketStatsDto?> GetTicketStatsAsync();
    Task<(bool Success, TicketDetailDto? CreatedTicket, string? ErrorMessage)> CreateTicketAsync(TicketDto ticket);
    Task<(bool Success, string? ErrorMessage)> UpdateTicketAsync(int id, TicketDto ticket);
    Task<(bool Success, string? ErrorMessage)> DeleteTicketAsync(int id);

    Task<(bool Success, TicketCommentDto? AddedComment, string? ErrorMessage)> AddCommentAsync(int ticketId,
        TicketCommentDto comment);

    // Métodos para actividades
    Task<(bool Success, TicketActivityDto? AddedActivity, string? ErrorMessage)> AddActivityAsync(int ticketId, TicketActivityDto activity);
    Task<(bool Success, TicketActivityDto? UpdatedActivity, string? ErrorMessage)> UpdateActivityAsync(int ticketId, int activityId, TicketActivityDto activity);
    Task<(bool Success, string? ErrorMessage)> DeleteActivityAsync(int ticketId, int activityId);
    Task<(bool Success, string? ErrorMessage)> CompleteActivityAsync(int ticketId, int activityId);
}