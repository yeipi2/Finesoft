using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace fs_front.Services;

public class UnauthorizedHandler : DelegatingHandler
{
    private readonly ISyncLocalStorageService _localStorage;
    private readonly NavigationManager _nav;

    public UnauthorizedHandler(ISyncLocalStorageService localStorage, NavigationManager nav)
    {
        _localStorage = localStorage;
        _nav = nav;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri?.AbsolutePath?.ToLowerInvariant() ?? "";

        // ✅ No interceptar 401 del flujo de auth (si no, rompes mensajes de "credenciales incorrectas")
        var isAuthEndpoint =
            path.Contains("/api/auth/login") ||
            path.Contains("/api/auth/register") ||
            path.Contains("/api/auth/refresh");

        var token = _localStorage.GetItem<string>("accessToken");
        var hadToken = !string.IsNullOrWhiteSpace(token);

        if (hadToken)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        if (!isAuthEndpoint && response.StatusCode == HttpStatusCode.Unauthorized && hadToken)
        {
            // sesión expirada o token inválido (solo si había token)
            _localStorage.RemoveItem("accessToken");
            _localStorage.RemoveItem("refreshToken");

            // ✅ OJO: evita forceLoad si puedes. Si tu app lo necesita, déjalo,
            // pero ya no afectará al login porque login está excluido.
            _nav.NavigateTo("/iniciar-sesion");
            // _nav.NavigateTo("/iniciar-sesion", forceLoad: true);
        }

        return response;
    }

}
