using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface ITicketService
{
    Task<IEnumerable<TicketDetailDto>> GetTicketsAsync(string? status = null, string? priority = null,
        int? serviceId = null);

    Task<TicketDetailDto?> GetTicketByIdAsync(int id);
    Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(TicketDto ticketDto, string createdByUserId);
    Task<ServiceResult<bool>> UpdateTicketAsync(int id, TicketDto ticketDto, string userId);
    Task<ServiceResult<bool>> DeleteTicketAsync(int id);
    Task<ServiceResult<TicketCommentDto>> AddCommentAsync(int ticketId, TicketCommentDto commentDto, string userId);
    Task<TicketStatsDto> GetTicketStatsAsync();
}