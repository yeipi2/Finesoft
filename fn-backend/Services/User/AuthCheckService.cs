using fs_backend.Repositories;
using fs_front.Services;
using System.Net.Http.Json;

namespace fs_front.Services;

public class AuthCheckService : IAuthCheckService
{
    private readonly HttpClient _http;
    private readonly ILogger<AuthCheckService> _logger;

    public AuthCheckService(HttpClient http, ILogger<AuthCheckService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null)
    {
        try
        {
            var url = $"api/users/check-email?email={Uri.EscapeDataString(email)}";

            if (!string.IsNullOrEmpty(excludeUserId))
                url += $"&excludeUserId={Uri.EscapeDataString(excludeUserId)}";

            var result = await _http.GetFromJsonAsync<EmailCheckResponse>(url);
            return result?.Available ?? true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error verificando email: {Error}", ex.Message);
            return true; // Si falla la red, dejamos pasar — el backend lo rechazará en el submit
        }
    }

    private record EmailCheckResponse(bool Available);
}