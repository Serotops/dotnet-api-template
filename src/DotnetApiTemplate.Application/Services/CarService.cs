using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Application.Interfaces.Services;
using DotnetApiTemplate.Domain.Enums;
using FluentResults;

namespace DotnetApiTemplate.Application.Services;

public class CarService(ICarRepository carRepository) : ICarService
{
    private readonly ICarRepository _carRepository = carRepository;

    public async Task<Result<CarDto>> GetByIdAsync(Guid id)
    {
        var car = await _carRepository.GetByIdAsync(id);

        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        return Result.Ok(CarDto.FromEntity(car));
    }

    public async Task<Result<IEnumerable<CarDto>>> GetAllAsync()
    {
        var cars = await _carRepository.GetAllAsync();
        return Result.Ok(cars.Select(CarDto.FromEntity));
    }

    public async Task<Result<PaginationResult<CarDto>>> GetFilteredAsync(CarParams filterParams)
    {
        var result = await _carRepository.GetFilteredAsync(filterParams);

        var paginationResult = new PaginationResult<CarDto>
        {
            PageIndex = result.PageIndex,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            Data = result.Data.Select(CarDto.FromEntity).ToList()
        };

        return Result.Ok(paginationResult);
    }

    public async Task<Result<Guid>> AddAsync(CarUpsertDto carDto)
    {
        var car = carDto.ToEntity();
        var createdCar = await _carRepository.AddAsync(car);
        return Result.Ok(createdCar.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, CarUpsertDto carDto)
    {
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        var result = Result.Ok();

        if (carDto.Mileage < existingCar.Mileage)
        {
            result.WithError(new BusinessRuleError(
                $"Mileage cannot decrease from {existingCar.Mileage} to {carDto.Mileage}.",
                ErrorCode.INVALID_MILEAGE));
        }

        if (result.IsFailed)
            return result;

        carDto.ApplyTo(existingCar);
        await _carRepository.UpdateAsync(existingCar);

        return Result.Ok();
    }

    public async Task<Result> PatchAsync(Guid id, CarPatchDto patchDto)
    {
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        var result = Result.Ok();

        if (patchDto.Mileage.HasValue && patchDto.Mileage.Value < existingCar.Mileage)
        {
            result.WithError(new BusinessRuleError(
                $"Mileage cannot decrease from {existingCar.Mileage} to {patchDto.Mileage.Value}.",
                ErrorCode.INVALID_MILEAGE));
        }

        if (result.IsFailed)
            return result;

        if (patchDto.Make != null)
            existingCar.Make = patchDto.Make;

        if (patchDto.Model != null)
            existingCar.Model = patchDto.Model;

        if (patchDto.Year.HasValue)
            existingCar.Year = patchDto.Year.Value;

        if (patchDto.Color != null)
            existingCar.Color = patchDto.Color;

        if (patchDto.Price.HasValue)
            existingCar.Price = patchDto.Price.Value;

        if (patchDto.VIN != null)
            existingCar.VIN = patchDto.VIN;

        if (patchDto.Mileage.HasValue)
            existingCar.Mileage = patchDto.Mileage.Value;

        if (patchDto.IsAvailable.HasValue)
            existingCar.IsAvailable = patchDto.IsAvailable.Value;

        await _carRepository.UpdateAsync(existingCar);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var car = await _carRepository.GetByIdAsync(id);

        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        await _carRepository.DeleteAsync(id);

        return Result.Ok();
    }

    public async Task<Result<string>> GenerateCarReportAsync(Guid id)
    {
        var car = await _carRepository.GetByIdAsync(id);
        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        var reportContent = $@"
            CAR REPORT
            ==========
            Make: {car.Make}
            Model: {car.Model}
            Year: {car.Year}
            Color: {car.Color}
            Price: ${car.Price:N2}
            VIN: {car.VIN ?? "N/A"}
            Mileage: {car.Mileage:N0} miles
            Available: {(car.IsAvailable ? "Yes" : "No")}
            Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
            ";

        try
        {
            var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            Directory.CreateDirectory(reportsDir);

            var fileName = $"car-report-{car.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
            var filePath = Path.Combine(reportsDir, fileName);

            await File.WriteAllTextAsync(filePath, reportContent);

            return Result.Ok(filePath)
                .WithSuccess($"Report generated successfully at {filePath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Fail(new DatabaseError(
                "Access denied when writing report to disk",
                ErrorCode.DATABASE_ERROR))
                .WithError(ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Fail(new DatabaseError(
                "Failed to write report to disk",
                ErrorCode.DATABASE_ERROR))
                .WithError(ex.Message);
        }
    }
}
