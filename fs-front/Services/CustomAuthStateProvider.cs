using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Blazored.LocalStorage;
using fs_front.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace fs_front.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider, IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ISyncLocalStorageService _localStorage;

        public CustomAuthStateProvider(HttpClient httpClient, ISyncLocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;

            var token = _localStorage.GetItem<string>("accessToken");
            if (token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            try
            {
                var response = await _httpClient.GetAsync("manage/info");
                if (response.IsSuccessStatusCode)
                {
                    var strResponse = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);
                    var email = jsonResponse?["email"]?.ToString();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, email ?? ""),
                        new Claim(ClaimTypes.Email, email ?? "")
                    };

                    var userResponse = await _httpClient.GetAsync($"api/users");
                    if (userResponse.IsSuccessStatusCode)
                    {
                        var usersJson = await userResponse.Content.ReadAsStringAsync();
                        var users = JsonNode.Parse(usersJson)?.AsArray();

                        var currentUser = users?.FirstOrDefault(u => u?["email"]?.ToString() == email);
                        if (currentUser != null)
                        {
                            var role = currentUser["roleName"]?.ToString();
                            if (!string.IsNullOrEmpty(role))
                            {
                                claims.Add(new Claim(ClaimTypes.Role, role));
                            }

                            var userId = currentUser["id"]?.ToString();
                            if (!string.IsNullOrEmpty(userId))
                            {
                                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                            }
                        }
                    }

                    var identity = new ClaimsIdentity(claims, "Token");
                    user = new ClaimsPrincipal(identity);
                    return new AuthenticationState(user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en autenticación: {ex.Message}");
            }

            return new AuthenticationState(user);
        }

        public async Task<FormResponse> LoginAsync(LoginModel loginModel)
        {
            try
            {
                var response =
                    await _httpClient.PostAsJsonAsync("login", new { loginModel.Email, loginModel.Password });

                if (response.IsSuccessStatusCode)
                {
                    var strResponse = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonNode.Parse(strResponse);
                    var accessToken = jsonResponse?["accessToken"]?.ToString();
                    var refreshToken = jsonResponse?["refreshToken"]?.ToString();

                    _localStorage.SetItem("accessToken", accessToken);
                    _localStorage.SetItem("refreshToken", refreshToken);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken);

                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

                    return new FormResponse { Succeeded = true };
                }
                else
                {
                    return new FormResponse { Succeeded = false, Errors = ["Correo o contraseña incorrecto"] };
                }
            }
            catch (Exception ex)
            {
                return new FormResponse { Succeeded = false, Errors = [ex.Message] };
            }
        }

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

        public void Logout()
        {
            _localStorage.RemoveItem("accessToken");
            _localStorage.RemoveItem("refreshToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}