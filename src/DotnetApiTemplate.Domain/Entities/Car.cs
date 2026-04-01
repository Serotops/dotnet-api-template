namespace DotnetApiTemplate.Domain.Entities;

public class Car : AuditableEntity
{
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required int Year { get; set; }
    public required string Color { get; set; }
    public decimal Price { get; set; }
    public string? VIN { get; set; }
    public int Mileage { get; set; }
    public bool IsAvailable { get; set; } = true;
}
