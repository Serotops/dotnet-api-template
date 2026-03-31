namespace DotnetApiTemplate.Application.DTOs;

/// <summary>
/// DTO for partially updating a Car entity.
/// All properties are nullable - only provided properties will be updated.
/// </summary>
public class CarPatchDto
{
    /// <summary>
    /// Car manufacturer/brand
    /// </summary>
    public string? Make { get; set; }

    /// <summary>
    /// Car model name
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Manufacturing year
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Car color
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Car price
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Vehicle Identification Number
    /// </summary>
    public string? VIN { get; set; }

    /// <summary>
    /// Current mileage in miles
    /// </summary>
    public int? Mileage { get; set; }

    /// <summary>
    /// Whether the car is available for sale
    /// </summary>
    public bool? IsAvailable { get; set; }
}
