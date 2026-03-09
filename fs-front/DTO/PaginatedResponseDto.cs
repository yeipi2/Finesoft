namespace fs_front.DTO;

public class PaginatedResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Pages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrev { get; set; }
}
