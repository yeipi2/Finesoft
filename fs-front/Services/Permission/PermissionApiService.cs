using fs_front.DTO;
using fs_front.Repositories;
using System.Net.Http.Json;

namespace fs_front.Services;

public class PermissionApiService : IPermissionApiService
{
    private readonly HttpClient _httpClient;

    public PermissionApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<PermissionDto>?> GetAllPermissionsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<PermissionDto>>("api/permissions");
            Console.WriteLine($"✅ Permisos obtenidos: {result?.Count ?? 0}");
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Error obteniendo permisos: {e.Message}");
            return null;
        }
    }

    public async Task<RolePermissionsDto?> GetRolePermissionsAsync(string roleId)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<RolePermissionsDto>($"api/permissions/role/{roleId}");
            Console.WriteLine($"✅ Permisos del rol {roleId} obtenidos: {result?.PermissionIds.Count ?? 0}");
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Error obteniendo permisos del rol {roleId}: {e.Message}");
            return null;
        }
    }

    public async Task<List<RolePermissionsDto>?> GetAllRolesPermissionsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<RolePermissionsDto>>("api/permissions/all-roles");
            Console.WriteLine($"✅ Roles obtenidos: {result?.Count ?? 0}");
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Error obteniendo permisos de todos los roles: {e.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> AssignPermissionsAsync(AssignPermissionsDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/permissions/assign", dto);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Permisos asignados correctamente al rol {dto.RoleId}");
                return (true, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"❌ Error al asignar permisos: {errorContent}");
            return (false, $"No se pudieron asignar los permisos: {errorContent}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Excepción al asignar permisos: {e.Message}");
            return (false, $"Error: {e.Message}");
        }
    }
}