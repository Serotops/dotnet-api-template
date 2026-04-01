using DotnetApiTemplate.Domain.Entities;

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

    public static CarDto FromEntity(Car car) => new()
    {
        Id = car.Id,
        Make = car.Make,
        Model = car.Model,
        Year = car.Year,
        Color = car.Color,
        Price = car.Price,
        VIN = car.VIN,
        Mileage = car.Mileage,
        IsAvailable = car.IsAvailable,
        CreatedAt = car.CreatedAt,
        ModifiedAt = car.ModifiedAt
    };
}
