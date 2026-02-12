// ============================================================
// ACTUALIZACIÓN 1: ITicketService.cs
// Agregar este método a la interfaz
// ============================================================

using fs_backend.DTO;
using fs_backend.Util;

namespace fs_backend.Repositories;

public interface ITicketService
{
    // Métodos existentes...
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
    Task<TicketStatsDto> GetTicketStatsAsync(string? userId = null, bool byCreator = false);

    // Métodos para actividades
    Task<ServiceResult<TicketActivityDto>> AddActivityAsync(int ticketId, TicketActivityDto activityDto, string userId);
    Task<ServiceResult<TicketActivityDto>> UpdateActivityAsync(int ticketId, int activityId, TicketActivityDto activityDto, string userId);
    Task<ServiceResult<bool>> DeleteActivityAsync(int ticketId, int activityId);
    Task<ServiceResult<bool>> CompleteActivityAsync(int ticketId, int activityId, string userId);

    // 🆕 NUEVO MÉTODO para actualizar solo el estado
    Task<ServiceResult<bool>> UpdateTicketStatusAsync(int ticketId, string newStatus, string userId);
}