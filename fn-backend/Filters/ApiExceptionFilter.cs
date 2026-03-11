using System.Diagnostics;
using System.Text.Json;
using fs_backend.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace fs_backend.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        _logger.LogError(context.Exception,
            "Excepción en controlador. Controller: {Controller}, Action: {Action}, TraceId: {TraceId}",
            context.RouteData.Values["controller"],
            context.RouteData.Values["action"],
            traceId);

        var errorResponse = MapException(context.Exception, traceId);

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = errorResponse.Status,
            ContentTypes = { "application/json" }
        };

        context.ExceptionHandled = true;
    }

    private ApiErrorResponse MapException(Exception exception, string traceId)
    {
        return exception switch
        {
            DbUpdateException dbEx => HandleDbUpdateException(dbEx, traceId),

            SecurityTokenExpiredException => ApiErrorResponse.Unauthorized("Token expirado", traceId),

            SecurityTokenInvalidSignatureException => ApiErrorResponse.Unauthorized("Token inválido", traceId),

            SecurityTokenException => ApiErrorResponse.Unauthorized("Token no válido", traceId),

            UnauthorizedAccessException => ApiErrorResponse.Unauthorized(exception.Message, traceId),

            ArgumentException argEx => ApiErrorResponse.BadRequest(argEx.Message, traceId),

            KeyNotFoundException => ApiErrorResponse.NotFound(exception.Message, traceId),

            InvalidOperationException invOpEx => ApiErrorResponse.BadRequest(invOpEx.Message, traceId),

            _ => ApiErrorResponse.InternalError(
                _env.IsDevelopment() ? exception.Message : "Error interno del servidor",
                traceId,
                _env.IsDevelopment())
        };
    }

    private ApiErrorResponse HandleDbUpdateException(DbUpdateException dbEx, string traceId)
    {
        var detail = "Error de base de datos";

        if (_env.IsDevelopment())
        {
            detail = dbEx.InnerException?.Message ?? dbEx.Message;
        }

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

        if (dbEx.InnerException?.Message.Contains("UNIQUE constraint") == true ||
            dbEx.InnerException?.Message.Contains("cannot insert duplicate key") == true)
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

        if (dbEx.InnerException?.Message.Contains("conflicted with the REFERENCE constraint") == true)
        {
            return new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/409",
                Title = "Conflict",
                Status = 409,
                Detail = "No se puede eliminar: el registro está siendo utilizado",
                TraceId = traceId
            };
        }

        return ApiErrorResponse.InternalError(detail, traceId, _env.IsDevelopment());
    }
}
