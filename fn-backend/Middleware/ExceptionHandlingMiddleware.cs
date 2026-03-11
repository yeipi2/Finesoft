using System.Diagnostics;
using System.Net;
using System.Text.Json;
using fs_backend.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace fs_backend.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no manejada. TraceId: {TraceId}", traceId);
            await HandleExceptionAsync(context, ex, traceId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            // Excepciones de Entity Framework
            DbUpdateException dbEx =>
                CreateDbErrorResponse(dbEx, traceId),

            // Excepciones de validación de JWT
            SecurityTokenExpiredException =>
                ApiErrorResponse.Unauthorized("Token expirado", traceId),

            SecurityTokenInvalidSignatureException =>
                ApiErrorResponse.Unauthorized("Token inválido", traceId),

            UnauthorizedAccessException =>
                ApiErrorResponse.Unauthorized(exception.Message, traceId),

            // Excepciones de Argumento/Invalid Operation
            ArgumentException argEx =>
                ApiErrorResponse.BadRequest(argEx.Message, traceId),

            InvalidOperationException invOpEx =>
                ApiErrorResponse.BadRequest(invOpEx.Message, traceId),

            // Excepciones de KeyNotFound (recurso no encontrado)
            KeyNotFoundException =>
                ApiErrorResponse.NotFound(exception.Message, traceId),

            // Por defecto, error 500
            _ =>
                ApiErrorResponse.InternalError(
                    _env.IsDevelopment() ? exception.Message : "Error interno",
                    traceId,
                    _env.IsDevelopment())
        };

        response.StatusCode = errorResponse.Status;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }

    private ApiErrorResponse CreateDbErrorResponse(DbUpdateException dbEx, string traceId)
    {
        var detail = "Error de base de datos";

        if (_env.IsDevelopment())
        {
            detail = dbEx.InnerException?.Message ?? dbEx.Message;
        }

        _logger.LogError(dbEx, "Error de base de datos: {Message}", dbEx.Message);

        if (dbEx.InnerException?.Message.Contains("REFERENCE constraint") == true)
        {
            return new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/409",
                Title = "Conflict",
                Status = 409,
                Detail = "No se puede eliminar: existen registros relacionados",
                TraceId = traceId
            };
        }

        if (dbEx.InnerException?.Message.Contains("UNIQUE constraint") == true)
        {
            return new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/409",
                Title = "Conflict",
                Status = 409,
                Detail = "Ya existe un registro con estos datos",
                TraceId = traceId
            };
        }

        return ApiErrorResponse.InternalError(detail, traceId, _env.IsDevelopment());
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
