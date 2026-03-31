namespace DotnetApiTemplate.Application.DTOs;

public class CarDto
{
    public Guid Id { get; set; }
    public string Make { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string Color { get; set; } = null!;
    public decimal Price { get; set; }
    public string? VIN { get; set; }
    public int Mileage { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
