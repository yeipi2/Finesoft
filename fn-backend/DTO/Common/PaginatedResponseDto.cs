namespace fs_backend.DTO.Common;

public class PaginatedResponseDto<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int Pages { get; init; }
    public bool HasNext => Page < Pages;
    public bool HasPrev => Page > 1;

    public static PaginatedResponseDto<T> Create(IReadOnlyList<T> items, int total, int page, int pageSize)
    {
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

        return new PaginatedResponseDto<T>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            Pages = pages
        };
    }
}
