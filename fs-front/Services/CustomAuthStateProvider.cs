using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Blazored.LocalStorage;
using fs_front.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace fs_front.Services
{
    // Proveedor de estado de autenticación personalizado
    // y servicio de autenticación (login/register/logout).
    public class CustomAuthStateProvider : AuthenticationStateProvider, IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ISyncLocalStorageService _localStorage;

        public CustomAuthStateProvider(HttpClient httpClient, ISyncLocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;

            // Al iniciar, si ya hay token guardado,
            // lo coloca en el header por defecto.
            var token = _localStorage.GetItem<string>("accessToken");
            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // Devuelve el estado de autenticación actual
        // leyendo el token JWT del localStorage.
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = _localStorage.GetItem<string>("accessToken");

            // Si no hay token, usuario anónimo.
            if (string.IsNullOrWhiteSpace(token))
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

            try
            {
                // Parsear el JWT para extraer los claims.
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var claims = jwt.Claims.ToList();

                // Crear identidad con los claims.
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                // asegura header para llamadas al backend
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return Task.FromResult(new AuthenticationState(user));
            }
            catch
            {
                // token inválido -> limpiar
                _localStorage.RemoveItem("accessToken");
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
        }

        // Hace login contra la API y guarda el token si es correcto.
        public async Task<FormResponse> LoginAsync(LoginModel loginModel)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    new { loginModel.Email, loginModel.Password });

                if (!response.IsSuccessStatusCode)
                    return new FormResponse { Succeeded = false, Errors = ["Correo o contraseña incorrecto"] };

                var strResponse = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonNode.Parse(strResponse);
                var accessToken = jsonResponse?["accessToken"]?.ToString();

                if (string.IsNullOrWhiteSpace(accessToken))
                    return new FormResponse { Succeeded = false, Errors = ["No se recibió token"] };

                // Guardar token y limpiar refresh viejo.
                _localStorage.SetItem("accessToken", accessToken);
                _localStorage.RemoveItem("refreshToken"); // por si quedaba basura anterior

                // Actualizar header para futuras llamadas.
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // Notificar a Blazor que cambió el estado de auth.
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

                return new FormResponse { Succeeded = true };
            }
            catch (Exception ex)
            {
                return new FormResponse { Succeeded = false, Errors = [ex.Message] };
            }
        }

        // Registra un usuario nuevo y, si es exitoso,
        // intenta loguearlo automáticamente.
        public async Task<FormResponse> RegisterAsync(RegisterModel registerModel)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("register", registerModel);
                if (response.IsSuccessStatusCode)
                {
                    var loginModel = new LoginModel
                    {
                        Email = registerModel.Email,
                        Password = registerModel.Password
                    };

                    return await LoginAsync(loginModel);
                }

                // Si falla, parsea los errores del backend.
                var strResponse = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonNode.Parse(strResponse);
                var errorsObject = jsonResponse?["errors"]?.AsObject();
                var errorList = new List<string>();
                foreach (var error in errorsObject)
                {
                    errorList.Add(error.Value![0].ToString());
                }

                var formsResult = new FormResponse
                {
                    Succeeded = false,
                    Errors = errorList.ToArray()
                };

                return formsResult;
            }
            catch (Exception ex)
            {
                return new FormResponse { Succeeded = false, Errors = [ex.Message] };
            }
        }

        // Cierra sesión: borra tokens y notifica a Blazor.
        public void Logout()
        {
            _localStorage.RemoveItem("accessToken");
            _localStorage.RemoveItem("refreshToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
