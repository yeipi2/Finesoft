using fs_front.DTO;

namespace fs_front.Services;

public interface ITicketApiService
{
    Task<List<TicketDetailDto>?> GetTicketsAsync(string? status = null, string? priority = null, int? serviceId = null);
    Task<TicketDetailDto?> GetTicketByIdAsync(int id);
    Task<TicketStatsDto?> GetTicketStatsAsync();
    Task<(bool Success, TicketDetailDto? CreatedTicket, string? ErrorMessage)> CreateTicketAsync(TicketDto ticket);
    Task<(bool Success, string? ErrorMessage)> UpdateTicketAsync(int id, TicketDto ticket);
    Task<(bool Success, string? ErrorMessage)> DeleteTicketAsync(int id);

    Task<(bool Success, TicketCommentDto? AddedComment, string? ErrorMessage)> AddCommentAsync(int ticketId,
        TicketCommentDto comment);
}