using System.Text.Json.Serialization;

namespace fs_backend.DTO;

public class ApiErrorResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "https://httpstatuses.com/500";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Internal Server Error";

    [JsonPropertyName("status")]
    public int Status { get; set; } = 500;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = "Ocurrió un error inesperado";

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiErrorResponse NotFound(string detail, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/404",
            Title = "Resource Not Found",
            Status = 404,
            Detail = detail,
            TraceId = traceId
        };
    }

    public static ApiErrorResponse BadRequest(string detail, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/400",
            Title = "Bad Request",
            Status = 400,
            Detail = detail,
            TraceId = traceId
        };
    }

    public static ApiErrorResponse Unauthorized(string detail = "No autorizado", string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/401",
            Title = "Unauthorized",
            Status = 401,
            Detail = detail,
            TraceId = traceId
        };
    }

    public static ApiErrorResponse Forbidden(string detail = "Acceso denegado", string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/403",
            Title = "Forbidden",
            Status = 403,
            Detail = detail,
            TraceId = traceId
        };
    }

    public static ApiErrorResponse ValidationError(Dictionary<string, string[]> errors, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/422",
            Title = "Validation Failed",
            Status = 422,
            Detail = "Los datos proporcionados no son válidos",
            Errors = errors,
            TraceId = traceId
        };
    }

    public static ApiErrorResponse InternalError(string detail, string? traceId = null, bool showDetails = false)
    {
        return new ApiErrorResponse
        {
            Type = "https://httpstatuses.com/500",
            Title = "Internal Server Error",
            Status = 500,
            Detail = showDetails ? detail : "Ocurrió un error inesperado. Contacte al administrador",
            TraceId = traceId
        };
    }
}
