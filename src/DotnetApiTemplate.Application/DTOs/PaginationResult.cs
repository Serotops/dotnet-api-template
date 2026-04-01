namespace DotnetApiTemplate.Application.DTOs;

public class PaginationResult<T>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public List<T> Data { get; set; } = [];
}
