namespace DotnetApiTemplate.Application.DTOs;

public class CarUpsertDto
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? VIN { get; set; }
    public int Mileage { get; set; }
    public bool IsAvailable { get; set; } = true;
}
