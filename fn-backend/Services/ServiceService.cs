//using fn_backend.DTO;
//using fn_backend.Models;
//using fs_backend.Identity;
//using fs_backend.Repositories;
//using fs_backend.Util;
//using Microsoft.EntityFrameworkCore;

//namespace fs_backend.Services;

//public class ServiceService : IServiceService
//{
//    private readonly ApplicationDbContext _context;

//    public ServiceService(ApplicationDbContext context)
//    {
//        _context = context;
//    }

//    public async Task<IEnumerable<ServiceDetailDto>> GetServicesAsync()
//    {
//        var services = await _context.Services
//            .Include(s => s.Project)
//            .Include(s => s.TypeService)
//            .Include(s => s.TypeActivity)
//            .ToListAsync();

//        return services.Select(MapToDetailDto);
//    }

//    public async Task<IEnumerable<ServiceDetailDto>> GetServicesByProjectAsync(int projectId)
//    {
//        var services = await _context.Services
//            .Include(s => s.Project)
//            .Include(s => s.TypeService)
//            .Include(s => s.TypeActivity)
//            .Where(s => s.ProjectId == projectId)
//            .ToListAsync();

//        return services.Select(MapToDetailDto);
//    }

//    public async Task<ServiceDetailDto?> GetServiceByIdAsync(int id)
//    {
//        var service = await _context.Services
//            .Include(s => s.Project)
//            .Include(s => s.TypeService)
//            .Include(s => s.TypeActivity)
//            .FirstOrDefaultAsync(s => s.Id == id);

//        return service == null ? null : MapToDetailDto(service);
//    }

//    public async Task<ServiceResult<ServiceDetailDto>> CreateServiceAsync(ServiceDto serviceDto)
//    {
//        // Validar que el proyecto existe
//        var projectExists = await _context.Projects.AnyAsync(p => p.Id == serviceDto.ProjectId);
//        if (!projectExists)
//        {
//            return ServiceResult<ServiceDetailDto>.Failure("El proyecto especificado no existe");
//        }

//        // Validar que el tipo de servicio existe
//        var typeServiceExists = await _context.TypeServices.AnyAsync(ts => ts.Id == serviceDto.TypeServiceId);
//        if (!typeServiceExists)
//        {
//            return ServiceResult<ServiceDetailDto>.Failure("El tipo de servicio especificado no existe");
//        }

//        // Validar que el tipo de actividad existe
//        var typeActivityExists = await _context.TypeActivities.AnyAsync(ta => ta.Id == serviceDto.TypeActivityId);
//        if (!typeActivityExists)
//        {
//            return ServiceResult<ServiceDetailDto>.Failure("El tipo de actividad especificado no existe");
//        }

//        var service = new Service
//        {
//            Name = serviceDto.Name,
//            Description = serviceDto.Description,
//            HourlyRate = serviceDto.HourlyRate,
//            ProjectId = serviceDto.ProjectId,
//            TypeServiceId = serviceDto.TypeServiceId,
//            TypeActivityId = serviceDto.TypeActivityId,
//            IsActive = serviceDto.IsActive
//        };

//        _context.Services.Add(service);
//        await _context.SaveChangesAsync();

//        // Cargar las relaciones para devolver el DTO completo
//        await _context.Entry(service).Reference(s => s.Project).LoadAsync();
//        await _context.Entry(service).Reference(s => s.TypeService).LoadAsync();
//        await _context.Entry(service).Reference(s => s.TypeActivity).LoadAsync();

//        return ServiceResult<ServiceDetailDto>.Success(MapToDetailDto(service));
//    }

//    public async Task<ServiceResult<bool>> UpdateServiceAsync(int id, ServiceDto serviceDto)
//    {
//        var service = await _context.Services.FindAsync(id);
//        if (service == null)
//        {
//            return ServiceResult<bool>.Failure("Servicio no encontrado");
//        }

//        // Validar que el proyecto existe
//        var projectExists = await _context.Projects.AnyAsync(p => p.Id == serviceDto.ProjectId);
//        if (!projectExists)
//        {
//            return ServiceResult<bool>.Failure("El proyecto especificado no existe");
//        }

//        // Validar que el tipo de servicio existe
//        var typeServiceExists = await _context.TypeServices.AnyAsync(ts => ts.Id == serviceDto.TypeServiceId);
//        if (!typeServiceExists)
//        {
//            return ServiceResult<bool>.Failure("El tipo de servicio especificado no existe");
//        }

//        // Validar que el tipo de actividad existe
//        var typeActivityExists = await _context.TypeActivities.AnyAsync(ta => ta.Id == serviceDto.TypeActivityId);
//        if (!typeActivityExists)
//        {
//            return ServiceResult<bool>.Failure("El tipo de actividad especificado no existe");
//        }

//        service.Name = serviceDto.Name;
//        service.Description = serviceDto.Description;
//        service.HourlyRate = serviceDto.HourlyRate;
//        service.ProjectId = serviceDto.ProjectId;
//        service.TypeServiceId = serviceDto.TypeServiceId;
//        service.TypeActivityId = serviceDto.TypeActivityId;
//        service.IsActive = serviceDto.IsActive;

//        _context.Services.Update(service);
//        await _context.SaveChangesAsync();

//        return ServiceResult<bool>.Success(true);
//    }

//    public async Task<ServiceResult<bool>> DeleteServiceAsync(int id)
//    {
//        var service = await _context.Services.FindAsync(id);
//        if (service == null)
//        {
//            return ServiceResult<bool>.Failure("Servicio no encontrado");
//        }

//        _context.Services.Remove(service);
//        await _context.SaveChangesAsync();

//        return ServiceResult<bool>.Success(true);
//    }

//    private ServiceDetailDto MapToDetailDto(Service service)
//    {
//        return new ServiceDetailDto
//        {
//            Id = service.Id,
//            Name = service.Name,
//            Description = service.Description,
//            HourlyRate = service.HourlyRate,
//            IsActive = service.IsActive,
//            ProjectId = service.ProjectId,
//            ProjectName = service.Project?.Name ?? string.Empty,
//            TypeServiceId = service.TypeServiceId,
//            TypeServiceName = service.TypeService?.Name ?? string.Empty,
//            TypeActivityId = service.TypeActivityId,
//            TypeActivityName = service.TypeActivity?.Name ?? string.Empty
//        };
//    }
//}