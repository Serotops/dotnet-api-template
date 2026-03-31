using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Application.Interfaces.Services;
using DotnetApiTemplate.Domain.Entities;
using DotnetApiTemplate.Domain.Enums;
using AutoMapper;
using FluentResults;

namespace DotnetApiTemplate.Application.Services;

public class CarService(ICarRepository carRepository, IMapper mapper) : ICarService
{
    private readonly ICarRepository _carRepository = carRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<Result<CarDto>> GetByIdAsync(Guid id)
    {
        var car = await _carRepository.GetByIdAsync(id);

        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        return Result.Ok(_mapper.Map<CarDto>(car));
    }

    public async Task<Result<IEnumerable<CarDto>>> GetAllAsync()
    {
        var cars = await _carRepository.GetAllAsync();
        return Result.Ok(_mapper.Map<IEnumerable<CarDto>>(cars));
    }

    public async Task<Result<PaginationResult<CarDto>>> GetFilteredAsync(CarParams filterParams)
    {
        var result = await _carRepository.GetFilteredAsync(filterParams);

        var paginationResult = new PaginationResult<CarDto>
        {
            PageIndex = result.PageIndex,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            Data = _mapper.Map<List<CarDto>>(result.Data)
        };

        return Result.Ok(paginationResult);
    }

    public async Task<Result<Guid>> AddAsync(CarUpsertDto carDto)
    {
        // Note: Basic validations (Year, Price, Mileage format/range) are already handled by FluentValidation
        // Only business rules that require context or database access should be validated here

        // Example: Add business rule validations here if needed:
        // - Check if VIN already exists (requires database query)
        // - Check if user has permission to add cars (requires context)
        // - Apply complex business rules that depend on multiple fields or external state

        // For now, we proceed directly to creating the entity
        var car = _mapper.Map<Car>(carDto);
        var createdCar = await _carRepository.AddAsync(car);
        return Result.Ok(createdCar.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, CarUpsertDto carDto)
    {
        // 1. Critical blocking validation - Early return pattern
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        // 2. Business rule validations - Collect all errors before returning
        // Note: Basic validations (Year, Price, Mileage format/range) are already handled by FluentValidation
        var result = Result.Ok();

        // Business rule: Mileage cannot decrease (odometer fraud prevention)
        if (carDto.Mileage < existingCar.Mileage)
        {
            result.WithError(new BusinessRuleError(
                $"Mileage cannot decrease from {existingCar.Mileage} to {carDto.Mileage}.",
                ErrorCode.INVALID_MILEAGE));
        }

        // Add more business rules here as needed:
        // - Example: Can't change year for existing cars
        // - Example: Price changes require approval if > 20% difference
        // - Example: Can't make unavailable if there are pending orders

        // If any business rules failed, return all errors at once
        if (result.IsFailed)
            return result;

        // 3. All validations passed - proceed with update
        _mapper.Map(carDto, existingCar);
        await _carRepository.UpdateAsync(existingCar);

        return Result.Ok();
    }

    public async Task<Result> PatchAsync(Guid id, CarPatchDto patchDto)
    {
        // 1. Critical blocking validation - Early return pattern
        var existingCar = await _carRepository.GetByIdAsync(id);
        if (existingCar == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        // 2. Business rule validations - Collect all errors before returning
        // Note: Basic validations (format/range) are already handled by FluentValidation on CarPatchDto
        var result = Result.Ok();

        // Business rule: Mileage cannot decrease (odometer fraud prevention)
        if (patchDto.Mileage.HasValue && patchDto.Mileage.Value < existingCar.Mileage)
        {
            result.WithError(new BusinessRuleError(
                $"Mileage cannot decrease from {existingCar.Mileage} to {patchDto.Mileage.Value}.",
                ErrorCode.INVALID_MILEAGE));
        }

        // Add more business rules here as needed

        // If any business rules failed, return all errors at once
        if (result.IsFailed)
            return result;

        // 3. All validations passed - proceed with partial update
        // Update only the properties that are provided (not null)
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

    /// <summary>
    /// Example method demonstrating when to STILL USE EXCEPTIONS (not Result pattern).
    /// This method generates a PDF report for a car by writing to the file system.
    ///
    /// When to use exceptions instead of Result:
    /// - Truly unexpected errors (filesystem failures, out of memory, database connection loss)
    /// - Infrastructure failures (external service unavailable, network errors)
    /// - Programming errors (null reference, index out of range)
    /// - System-level failures that you can't reasonably recover from
    ///
    /// When to use Result pattern:
    /// - Expected business failures (resource not found, validation errors)
    /// - Business rule violations (invalid year, negative price)
    /// - User input errors (missing required fields)
    /// - Any failure that is part of the normal application flow
    /// </summary>
    public async Task<Result<string>> GenerateCarReportAsync(Guid id)
    {
        // Step 1: Use Result pattern for expected failures (car not found)
        var car = await _carRepository.GetByIdAsync(id);
        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        // Step 2: Generate report content
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
            // Step 3: Write to file system
            // If this fails (disk full, permission denied, etc.), it throws an exception
            // This is appropriate because these are UNEXPECTED infrastructure failures
            var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            Directory.CreateDirectory(reportsDir); // Could throw UnauthorizedAccessException, IOException

            var fileName = $"car-report-{car.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
            var filePath = Path.Combine(reportsDir, fileName);

            await File.WriteAllTextAsync(filePath, reportContent); // Could throw IOException, UnauthorizedAccessException

            // Return success with the file path
            return Result.Ok(filePath)
                .WithSuccess($"Report generated successfully at {filePath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            // Catch specific expected exceptions and convert to Result
            // But in a real scenario, you might let these bubble up to be handled by exception middleware
            return Result.Fail(new DatabaseError(
                "Access denied when writing report to disk",
                ErrorCode.DATABASE_ERROR))
                .WithError(ex.Message);
        }
        catch (IOException ex)
        {
            // IO exceptions during file operations
            return Result.Fail(new DatabaseError(
                "Failed to write report to disk",
                ErrorCode.DATABASE_ERROR))
                .WithError(ex.Message);
        }
        // Note: Other exceptions (OutOfMemoryException, etc.) will bubble up and be caught by ExceptionHandlingMiddleware
    }
}
