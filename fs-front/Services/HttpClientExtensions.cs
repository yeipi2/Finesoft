using System.Text.Json;
using fs_front.DTO;

namespace fs_front.Services;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<List<T>?> GetListFromPagedEndpointAsync<T>(this HttpClient httpClient, string url)
    {
        using var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<T>();
        }

        try
        {
            var paged = JsonSerializer.Deserialize<PaginatedResponseDto<T>>(content, JsonOptions);
            if (paged?.Items is not null)
            {
                return paged.Items;
            }
        }
        catch (JsonException)
        {
        }

        return JsonSerializer.Deserialize<List<T>>(content, JsonOptions) ?? new List<T>();
    }
}
