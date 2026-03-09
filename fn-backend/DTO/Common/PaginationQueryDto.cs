namespace fs_backend.DTO.Common;

public class PaginationQueryDto
{
    private const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Sort { get; set; }

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
}
