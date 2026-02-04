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
        // Adjunta token SI existe (así ya no dependes de DefaultRequestHeaders)
        var token = _localStorage.GetItem<string>("accessToken");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // sesión expirada o token inválido -> limpiar y redirigir
            _localStorage.RemoveItem("accessToken");
            _localStorage.RemoveItem("refreshToken");

            // fuerza recarga para resetear estado visual y AuthorizeView
            _nav.NavigateTo("/iniciar-sesion", forceLoad: true);
        }

        return response;
    }
}
