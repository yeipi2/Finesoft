using fs_backend.DTO;
using fs_backend.Identity;
using fs_backend.Models;
using fs_backend.Repositories;
using fs_backend.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace fs_backend.Services;

public class TicketService : ITicketService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public TicketService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IEnumerable<TicketDetailDto>> GetTicketsAsync(string? status = null, string? priority = null,
        int? serviceId = null, string? userId = null)
    {
        var query = _context.Tickets
            .Include(t => t.Service)
            .ThenInclude(s => s.Project)
            .ThenInclude(p => p.Client)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Include(t => t.Activities)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (serviceId.HasValue)
        {
            query = query.Where(t => t.ServiceId == serviceId.Value);
        }

        // Filtrar por usuario asignado si se proporciona
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.AssignedToUserId == userId);
        }

        var tickets = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        var ticketDtos = new List<TicketDetailDto>();

        foreach (var ticket in tickets)
        {
            ticketDtos.Add(await MapToDetailDto(ticket));
        }

        return ticketDtos;
    }

    public async Task<TicketDetailDto?> GetTicketByIdAsync(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Service)
            .ThenInclude(s => s.Project)
            .ThenInclude(p => p.Client)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Include(t => t.History)
            .Include(t => t.Activities)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return null;
        }

        return await MapToDetailDto(ticket);
    }

    public async Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(TicketDto ticketDto, string createdByUserId)
    {
        var serviceExists = await _context.Services.AnyAsync(s => s.Id == ticketDto.ServiceId);
        if (!serviceExists)
        {
            return ServiceResult<TicketDetailDto>.Failure("El servicio especificado no existe");
        }

        if (!string.IsNullOrEmpty(ticketDto.AssignedToUserId))
        {
            var userExists = await _userManager.FindByIdAsync(ticketDto.AssignedToUserId);
            if (userExists == null)
            {
                return ServiceResult<TicketDetailDto>.Failure("El usuario asignado no existe");
            }
        }

        var ticket = new Ticket
        {
            Title = ticketDto.Title,
            Description = ticketDto.Description,
            ServiceId = ticketDto.ServiceId,
            Status = ticketDto.Status,
            Priority = ticketDto.Priority,
            AssignedToUserId = ticketDto.AssignedToUserId,
            CreatedByUserId = createdByUserId,
            EstimatedHours = ticketDto.EstimatedHours,
            ActualHours = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);

        var history = new TicketHistory
        {
            Ticket = ticket,
            UserId = createdByUserId,
            Action = "Created",
            NewValue = "Ticket creado",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        //load relationships
        await _context.Entry(ticket)
            .Reference(t => t.Service)
            .Query()
            .Include(s => s.Project)
            .ThenInclude(p => p.Client)
            .LoadAsync();

        return ServiceResult<TicketDetailDto>.Success(await MapToDetailDto(ticket));
    }

    public async Task<ServiceResult<bool>> UpdateTicketAsync(int id, TicketDto ticketDto, string userId)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Service)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        var serviceExists = await _context.Services.AnyAsync(s => s.Id == ticketDto.ServiceId);
        if (!serviceExists)
        {
            return ServiceResult<bool>.Failure("El servicio especificado no existe");
        }

        if (!string.IsNullOrEmpty(ticketDto.AssignedToUserId))
        {
            var userExists = await _userManager.FindByIdAsync(ticketDto.AssignedToUserId);
            if (userExists == null)
            {
                return ServiceResult<bool>.Failure("El usuario asignado no existe");
            }
        }

        //track changes for history
        var changes = new List<TicketHistory>();

        if (ticket.Title != ticketDto.Title)
        {
            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "TitleChanged",
                OldValue = ticket.Title,
                NewValue = ticketDto.Title,
                ChangedAt = DateTime.UtcNow
            });
        }

        if (ticket.Status != ticketDto.Status)
        {
            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "StatusChanged",
                OldValue = ticket.Status,
                NewValue = ticketDto.Status,
                ChangedAt = DateTime.UtcNow
            });

            //if closed, set closed date
            if (ticketDto.Status == "Cerrado")
            {
                ticket.ClosedAt = DateTime.UtcNow;
            }
        }

        if (ticket.Priority != ticketDto.Priority)
        {
            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "PriorityChanged",
                OldValue = ticket.Priority,
                NewValue = ticketDto.Priority,
                ChangedAt = DateTime.UtcNow
            });
        }

        if (ticket.AssignedToUserId != ticketDto.AssignedToUserId)
        {
            var oldUserName = ticket.AssignedToUserId != null
                ? (await _userManager.FindByIdAsync(ticket.AssignedToUserId))?.UserName
                : "Sin asignar";
            var newUserName = ticketDto.AssignedToUserId != null
                ? (await _userManager.FindByIdAsync(ticketDto.AssignedToUserId))?.UserName
                : "Sin asignar";

            changes.Add(new TicketHistory
            {
                TicketId = id,
                UserId = userId,
                Action = "AssignedToChanged",
                OldValue = oldUserName,
                NewValue = newUserName,
                ChangedAt = DateTime.UtcNow
            });
        }

        ticket.Title = ticketDto.Title;
        ticket.Description = ticketDto.Description;
        ticket.ServiceId = ticketDto.ServiceId;
        ticket.Status = ticketDto.Status;
        ticket.Priority = ticketDto.Priority;
        ticket.AssignedToUserId = ticketDto.AssignedToUserId;
        ticket.EstimatedHours = ticketDto.EstimatedHours;
        ticket.ActualHours = ticketDto.ActualHours;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.Tickets.Update(ticket);

        if (changes.Any())
        {
            _context.Set<TicketHistory>().AddRange(changes);
        }

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteTicketAsync(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);

        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<TicketCommentDto>> AddCommentAsync(int ticketId, TicketCommentDto commentDto,
        string userId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);

        if (ticket == null)
        {
            return ServiceResult<TicketCommentDto>.Failure("Ticket no encontrado");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<TicketCommentDto>.Failure("Usuario no encontrado");
        }

        var comment = new TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            Comment = commentDto.Comment,
            IsInternal = commentDto.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<TicketComment>().Add(comment);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "CommentAdded",
            NewValue = commentDto.IsInternal ? "Comentario interno agregado" : "Comentario agregado",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        return ServiceResult<TicketCommentDto>.Success(new TicketCommentDto
        {
            Id = comment.Id,
            Comment = comment.Comment,
            IsInternal = comment.IsInternal,
            UserId = userId,
            UserName = user.UserName,
            CreatedAt = comment.CreatedAt
        });
    }

    public async Task<TicketStatsDto> GetTicketStatsAsync(string? userId = null)
    {
        var query = _context.Tickets.AsQueryable();

        // Si se proporciona userId, filtrar solo tickets asignados a ese usuario
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.AssignedToUserId == userId);
        }

        var tickets = await query.ToListAsync();

        return new TicketStatsDto
        {
            Open = tickets.Count(t => t.Status == "Abierto"),
            InProgress = tickets.Count(t => t.Status == "En Progreso"),
            InReview = tickets.Count(t => t.Status == "En Revisión"),
            Closed = tickets.Count(t => t.Status == "Cerrado"),
            Total = tickets.Count,
            TotalEstimatedHours = tickets.Sum(t => t.EstimatedHours),
            TotalActualHours = tickets.Sum(t => t.ActualHours)
        };
    }

    // ========== MÉTODOS PARA ACTIVIDADES ==========

    public async Task<ServiceResult<TicketActivityDto>> AddActivityAsync(int ticketId, TicketActivityDto activityDto, string userId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Ticket no encontrado");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Usuario no encontrado");
        }

        var activity = new TicketActivity
        {
            TicketId = ticketId,
            Description = activityDto.Description,
            HoursSpent = activityDto.HoursSpent,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.Set<TicketActivity>().Add(activity);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "ActivityAdded",
            NewValue = $"Actividad agregada: {activityDto.Description}",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        return ServiceResult<TicketActivityDto>.Success(new TicketActivityDto
        {
            Id = activity.Id,
            TicketId = activity.TicketId,
            Description = activity.Description,
            HoursSpent = activity.HoursSpent,
            IsCompleted = activity.IsCompleted,
            CreatedAt = activity.CreatedAt,
            CompletedAt = activity.CompletedAt,
            CreatedByUserId = userId,
            CreatedByUserName = user.UserName ?? string.Empty
        });
    }

    public async Task<ServiceResult<TicketActivityDto>> UpdateActivityAsync(int ticketId, int activityId, TicketActivityDto activityDto, string userId)
    {
        var activity = await _context.Set<TicketActivity>()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TicketId == ticketId);

        if (activity == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Actividad no encontrada");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<TicketActivityDto>.Failure("Usuario no encontrado");
        }

        activity.Description = activityDto.Description;
        activity.HoursSpent = activityDto.HoursSpent;

        _context.Set<TicketActivity>().Update(activity);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "ActivityUpdated",
            NewValue = $"Actividad actualizada: {activityDto.Description}",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        return ServiceResult<TicketActivityDto>.Success(new TicketActivityDto
        {
            Id = activity.Id,
            TicketId = activity.TicketId,
            Description = activity.Description,
            HoursSpent = activity.HoursSpent,
            IsCompleted = activity.IsCompleted,
            CreatedAt = activity.CreatedAt,
            CompletedAt = activity.CompletedAt,
            CreatedByUserId = activity.CreatedByUserId,
            CreatedByUserName = user.UserName ?? string.Empty
        });
    }

    public async Task<ServiceResult<bool>> DeleteActivityAsync(int ticketId, int activityId)
    {
        var activity = await _context.Set<TicketActivity>()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TicketId == ticketId);

        if (activity == null)
        {
            return ServiceResult<bool>.Failure("Actividad no encontrada");
        }

        // Si la actividad está completada, restar sus horas del ticket
        if (activity.IsCompleted)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.ActualHours -= activity.HoursSpent;
                _context.Tickets.Update(ticket);
            }
        }

        _context.Set<TicketActivity>().Remove(activity);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> CompleteActivityAsync(int ticketId, int activityId, string userId)
    {
        var activity = await _context.Set<TicketActivity>()
            .FirstOrDefaultAsync(a => a.Id == activityId && a.TicketId == ticketId);

        if (activity == null)
        {
            return ServiceResult<bool>.Failure("Actividad no encontrada");
        }

        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            return ServiceResult<bool>.Failure("Ticket no encontrado");
        }

        // Marcar como completada
        activity.IsCompleted = true;
        activity.CompletedAt = DateTime.UtcNow;

        // Sumar las horas al total de horas reales del ticket
        ticket.ActualHours += activity.HoursSpent;

        _context.Set<TicketActivity>().Update(activity);
        _context.Tickets.Update(ticket);

        var history = new TicketHistory
        {
            TicketId = ticketId,
            UserId = userId,
            Action = "ActivityCompleted",
            NewValue = $"Actividad completada: {activity.Description} ({activity.HoursSpent}h)",
            ChangedAt = DateTime.UtcNow
        };

        _context.Set<TicketHistory>().Add(history);

        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    private async Task<TicketDetailDto> MapToDetailDto(Ticket ticket)
    {
        IdentityUser? assignedUser = null;
        IdentityUser? createdByUser = null;

        if (!string.IsNullOrEmpty(ticket.AssignedToUserId))
        {
            assignedUser = await _userManager.FindByIdAsync(ticket.AssignedToUserId);
        }

        if (!string.IsNullOrEmpty(ticket.CreatedByUserId))
        {
            createdByUser = await _userManager.FindByIdAsync(ticket.CreatedByUserId);
        }

        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            ServiceId = ticket.ServiceId,
            ServiceName = ticket.Service?.Name ?? string.Empty,
            ProjectId = ticket.Service?.ProjectId ?? 0,
            ProjectName = ticket.Service?.Project?.Name ?? string.Empty,
            ClientName = ticket.Service?.Project?.Client?.CompanyName ?? string.Empty,
            Status = ticket.Status,
            Priority = ticket.Priority,
            AssignedToUserId = ticket.AssignedToUserId,
            AssignedToUserName = assignedUser?.UserName,
            CreatedByUserId = ticket.CreatedByUserId,
            CreatedByUserName = createdByUser?.UserName ?? string.Empty,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ClosedAt = ticket.ClosedAt,
            EstimatedHours = ticket.EstimatedHours,
            ActualHours = ticket.ActualHours,
            HourlyRate = ticket.Service?.HourlyRate ?? 0
        };

        //map comments
        if (ticket.Comments != null && ticket.Comments.Any())
        {
            dto.Comments = new List<TicketCommentDto>();
            foreach (var comment in ticket.Comments.OrderBy(c => c.CreatedAt))
            {
                var commentUser = await _userManager.FindByIdAsync(comment.UserId);
                dto.Comments.Add(new TicketCommentDto
                {
                    Id = comment.Id,
                    Comment = comment.Comment,
                    IsInternal = comment.IsInternal,
                    UserId = comment.UserId,
                    UserName = commentUser?.UserName,
                    CreatedAt = comment.CreatedAt
                });
            }
        }

        //map attachments
        if (ticket.Attachments != null && ticket.Attachments.Any())
        {
            dto.Attachments = new List<TicketAttachmentDto>();
            foreach (var attachment in ticket.Attachments)
            {
                var uploadUser = await _userManager.FindByIdAsync(attachment.UploadedByUserId);
                dto.Attachments.Add(new TicketAttachmentDto
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath,
                    FileType = attachment.FileType,
                    FileSize = attachment.FileSize,
                    UploadedByUserName = uploadUser?.UserName ?? string.Empty,
                    UploadedAt = attachment.UploadedAt
                });
            }
        }

        //map history
        if (ticket.History != null && ticket.History.Any())
        {
            dto.History = new List<TicketHistoryDto>();
            foreach (var history in ticket.History.OrderByDescending(h => h.ChangedAt))
            {
                var historyUser = await _userManager.FindByIdAsync(history.UserId);
                dto.History.Add(new TicketHistoryDto
                {
                    Id = history.Id,
                    UserName = historyUser?.UserName ?? string.Empty,
                    Action = history.Action,
                    OldValue = history.OldValue,
                    NewValue = history.NewValue,
                    ChangedAt = history.ChangedAt
                });
            }
        }

        //map activities
        if (ticket.Activities != null && ticket.Activities.Any())
        {
            dto.Activities = new List<TicketActivityDto>();
            foreach (var activity in ticket.Activities.OrderBy(a => a.CreatedAt))
            {
                var activityUser = await _userManager.FindByIdAsync(activity.CreatedByUserId);
                dto.Activities.Add(new TicketActivityDto
                {
                    Id = activity.Id,
                    TicketId = activity.TicketId,
                    Description = activity.Description,
                    HoursSpent = activity.HoursSpent,
                    IsCompleted = activity.IsCompleted,
                    CreatedAt = activity.CreatedAt,
                    CompletedAt = activity.CompletedAt,
                    CreatedByUserId = activity.CreatedByUserId,
                    CreatedByUserName = activityUser?.UserName ?? string.Empty
                });
            }
        }

        return dto;
    }
}