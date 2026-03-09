using fs_backend.DTO.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace fs_backend.Util;

public static class ApiResponseHelper
{
    public static ObjectResult ToValidationProblem(this ControllerBase controller, IEnumerable<string> errors, int statusCode = StatusCodes.Status422UnprocessableEntity)
    {
        var modelState = new ModelStateDictionary();

        foreach (var error in errors.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            modelState.AddModelError(string.Empty, error);
        }

        var details = new ValidationProblemDetails(modelState)
        {
            Title = "Validation failed",
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = "One or more business rules were violated."
        };

        return new ObjectResult(details)
        {
            StatusCode = statusCode
        };
    }

    public static ObjectResult ToProblem(this ControllerBase controller, int statusCode, string title, string detail)
    {
        var details = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = controller.HttpContext.Request.Path
        };

        return new ObjectResult(details)
        {
            StatusCode = statusCode
        };
    }

    public static PaginatedResponseDto<T> Paginate<T>(
        IEnumerable<T> source,
        PaginationQueryDto query,
        Func<T, string, bool>? searchPredicate = null)
    {
        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;

        var filtered = source;
        if (!string.IsNullOrWhiteSpace(query.Search) && searchPredicate is not null)
        {
            filtered = filtered.Where(item => searchPredicate(item, query.Search!));
        }

        filtered = ApplySort(filtered, query.Sort);

        var total = filtered.Count();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return PaginatedResponseDto<T>.Create(items, total, page, pageSize);
    }

    private static IEnumerable<T> ApplySort<T>(IEnumerable<T> source, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return source;
        }

        var isDesc = sort.StartsWith("-", StringComparison.Ordinal);
        var propertyName = isDesc ? sort[1..] : sort;

        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return source;
        }

        var property = typeof(T).GetProperties()
            .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (property is null)
        {
            return source;
        }

        return isDesc
            ? source.OrderByDescending(x => property.GetValue(x))
            : source.OrderBy(x => property.GetValue(x));
    }
}
