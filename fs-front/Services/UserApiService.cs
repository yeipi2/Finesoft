using System.Net.Http.Json;
using fs_front.DTO;

namespace fs_front.Services;

public class UserApiService : IUserApiService
{
    private readonly HttpClient _httpClient;

    public UserApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UserDto>?> GetUsersAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<UserDto>>("api/users");
        }
        catch (HttpRequestException) 
        {
            return null;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>($"api/users/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<(bool Success, UserDto? CreatedUser, string? ErrorMessage)> CreateUserAsync(
        UserDto user)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users", user);

        if (response.IsSuccessStatusCode)
        {
            var createdUser = await response.Content.ReadFromJsonAsync<UserDto>();
            return (true, createdUser, null);
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        return (false, null, errorContent);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(string id, UserDto user)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/users/{id}", user);

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        return (false, errorContent);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteUserAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"api/users/{id}");

        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        return (false, errorContent);
    }

    public async Task<ProfileDto?> GetMyProfileAsync()
    {
        try { return await _httpClient.GetFromJsonAsync<ProfileDto>("api/users/me"); }
        catch { return null; }
    }

    public async Task<(bool Success, string? Error)> UpdateMyProfileAsync(ProfileUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("api/users/me", dto);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await response.Content.ReadAsStringAsync());
    }

    public async Task<(bool Success, string? Error)> ChangeMyPasswordAsync(ChangePasswordDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/users/me/change-password", dto);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await response.Content.ReadAsStringAsync());
    }

    // public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(string id, ChangePasswordDto passwords)
    // {
    //     var response = await _httpClient.PostAsJsonAsync($"api/users/{id}/change-password", passwords);
    //     
    //     if (response.IsSuccessStatusCode)
    //     {
    //         return (true, null);
    //     }
    //
    //     var errorContent = await response.Content.ReadAsStringAsync();
    //     return (false, errorContent);
    // }
    //
    // public async Task<List<string>?> GetRolesAsync()
    // {
    //     try
    //     {
    //         return await _httpClient.GetFromJsonAsync<List<string>>("api/users/roles");
    //     }
    //     catch (HttpRequestException)
    //     {
    //         return null;
    //     }
    // }
}