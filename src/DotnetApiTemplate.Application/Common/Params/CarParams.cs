namespace DotnetApiTemplate.Application.Common.Params;

public class CarParams : PaginationParams
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }
}
