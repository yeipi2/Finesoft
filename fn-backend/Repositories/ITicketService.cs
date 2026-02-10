using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface ITicketService
{
    // 🆕 Agregado parámetro byCreator
    Task<IEnumerable<TicketDetailDto>> GetTicketsAsync(
        string? status = null,
        string? priority = null,
        int? serviceId = null,
        string? userId = null,
        bool byCreator = false);

    Task<TicketDetailDto?> GetTicketByIdAsync(int id);
    Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(TicketDto ticketDto, string createdByUserId);
    Task<ServiceResult<bool>> UpdateTicketAsync(int id, TicketDto ticketDto, string userId);
    Task<ServiceResult<bool>> DeleteTicketAsync(int id);
    Task<ServiceResult<TicketCommentDto>> AddCommentAsync(int ticketId, TicketCommentDto commentDto, string userId);

    // 🆕 Agregado parámetro byCreator
    Task<TicketStatsDto> GetTicketStatsAsync(string? userId = null, bool byCreator = false);

    // Métodos para actividades
    Task<ServiceResult<TicketActivityDto>> AddActivityAsync(int ticketId, TicketActivityDto activityDto, string userId);
    Task<ServiceResult<TicketActivityDto>> UpdateActivityAsync(int ticketId, int activityId, TicketActivityDto activityDto, string userId);
    Task<ServiceResult<bool>> DeleteActivityAsync(int ticketId, int activityId);
    Task<ServiceResult<bool>> CompleteActivityAsync(int ticketId, int activityId, string userId);
}