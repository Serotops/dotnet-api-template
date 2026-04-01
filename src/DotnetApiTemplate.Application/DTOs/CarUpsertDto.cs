using DotnetApiTemplate.Domain.Entities;

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

    public Car ToEntity() => new()
    {
        Make = Make,
        Model = Model,
        Year = Year,
        Color = Color,
        Price = Price,
        VIN = VIN,
        Mileage = Mileage,
        IsAvailable = IsAvailable
    };

    public void ApplyTo(Car car)
    {
        car.Make = Make;
        car.Model = Model;
        car.Year = Year;
        car.Color = Color;
        car.Price = Price;
        car.VIN = VIN;
        car.Mileage = Mileage;
        car.IsAvailable = IsAvailable;
    }
}
