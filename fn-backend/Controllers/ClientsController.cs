using Asp.Versioning;
using fn_backend.DTO;
using fs_backend.Services;
using fs_backend.Attributes;
using fs_backend.DTO.Common;
using fs_backend.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace fs_backend.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IClientService clientService, ILogger<ClientsController> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

    /// <summary>
    /// GET: api/clients
    /// ⭐ CAMBIO: Cualquier usuario autenticado puede ver la lista (para dropdowns)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetClients([FromQuery] PaginationQueryDto query, [FromQuery] string? status = null)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} obtaining clientes", userId);

        var sortDescending = string.IsNullOrEmpty(query.Sort) || !query.Sort.StartsWith("-");
        var sortField = sortDescending ? query.Sort : query.Sort.Substring(1);

        var (clients, total) = await _clientService.GetClientsPaginatedAsync(
            search: query.Search,
            status: status,
            sortField: string.IsNullOrEmpty(sortField) ? "companyName" : sortField,
            sortDescending: sortDescending,
            page: query.NormalizedPage,
            pageSize: query.NormalizedPageSize
        );

        var pagedResult = PaginatedResponseDto<ClientDto>.Create(clients, total, query.NormalizedPage, query.NormalizedPageSize);
        return Ok(pagedResult);
    }

    /// <summary>
    /// GET: api/clients/{id}
    /// Requiere permiso: clients.view_detail
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("clients.view_detail")]
    public async Task<IActionResult> GetClientById(int id)
    {
        var client = await _clientService.GetClientByIdAsync(id);
        if (client == null)
        {
            return this.ToProblem(StatusCodes.Status404NotFound, "Resource not found", "Cliente no encontrado");
        }

        return Ok(client);
    }

    /// <summary>
    /// POST: api/clients
    /// Requiere permiso: clients.create
    /// </summary>
    [HttpPost]
    [RequirePermission("clients.create")]
    public async Task<IActionResult> CreateClient(ClientDto clientDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} creando cliente", userId);

        var result = await _clientService.CreateClientAsync(clientDto);
        if (!result.Succeeded)
        {
            return this.ToValidationProblem(result.Errors);
        }

        return CreatedAtAction(nameof(GetClientById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// PUT: api/clients/{id}
    /// Requiere permiso: clients.edit
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("clients.edit")]
    public async Task<IActionResult> UpdateClient(int id, ClientDto clientDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} actualizando cliente {ClientId}", userId, id);

        var result = await _clientService.UpdateClientAsync(id, clientDto);
        if (!result.Succeeded)
        {
            return this.ToProblem(StatusCodes.Status400BadRequest, "Bad Request", result.Errors.FirstOrDefault() ?? "Error al actualizar");
        }

        return NoContent();
    }

    /// <summary>
    /// DELETE: api/clients/{id}
    /// Requiere permiso: clients.delete
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("clients.delete")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("✅ Usuario {UserId} eliminando cliente {ClientId}", userId, id);

        var result = await _clientService.DeleteClientAsync(id);
        if (!result.Succeeded)
        {
            return this.ToProblem(StatusCodes.Status400BadRequest, "Bad Request", result.Errors.FirstOrDefault() ?? "Error al eliminar");
        }

        return NoContent();
    }

    /// <summary>
    /// GET: api/clients/search
    /// ⭐ Cualquier usuario autenticado puede buscar clientes (para autocompletado)
    /// </summary>
    [HttpGet("search")]
    [Authorize]
    public async Task<IActionResult> SearchClients([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new List<ClientDto>());
        }

        var clients = await _clientService.SearchClientsAsync(query);
        return Ok(clients);
    }

    /// <summary>
    /// PATCH: api/clients/{id}/toggle-status
    /// Alterna el estado activo/inactivo de un cliente
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleClientStatus(int id)
    {
        var result = await _clientService.DeleteClientAsync(id);
        if (!result.Succeeded)
        {
            return this.ToProblem(StatusCodes.Status400BadRequest, "Bad Request", result.Errors.FirstOrDefault() ?? "Error al cambiar estado");
        }

        return Ok(new { message = "Estado actualizado correctamente" });
    }
}
