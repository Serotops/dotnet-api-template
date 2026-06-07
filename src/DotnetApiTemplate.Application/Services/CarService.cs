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

    public async Task<Result<CarDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(id, cancellationToken);

        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        return Result.Ok(CarDto.FromEntity(car));
    }

    public async Task<Result<IEnumerable<CarDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cars = await _carRepository.GetAllAsync(cancellationToken);
        return Result.Ok(cars.Select(CarDto.FromEntity));
    }

    public async Task<Result<PaginationResult<CarDto>>> GetFilteredAsync(CarParams filterParams, CancellationToken cancellationToken = default)
    {
        var result = await _carRepository.GetFilteredAsync(filterParams, cancellationToken);

        var paginationResult = new PaginationResult<CarDto>
        {
            PageIndex = result.PageIndex,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            Data = result.Data.Select(CarDto.FromEntity).ToList()
        };

        return Result.Ok(paginationResult);
    }

    public async Task<Result<Guid>> AddAsync(CarUpsertDto car, CancellationToken cancellationToken = default)
    {
        var entity = car.ToEntity();
        var createdCar = await _carRepository.AddAsync(entity, cancellationToken);
        return Result.Ok(createdCar.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, CarUpsertDto car, CancellationToken cancellationToken = default)
    {
        var existingCar = await _carRepository.GetByIdAsync(id, cancellationToken);
        if (existingCar == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        var result = Result.Ok();

        if (car.Mileage < existingCar.Mileage)
        {
            result.WithError(new BusinessRuleError(
                $"Mileage cannot decrease from {existingCar.Mileage} to {car.Mileage}.",
                ErrorCode.INVALID_MILEAGE));
        }

        if (result.IsFailed)
            return result;

        car.ApplyTo(existingCar);
        await _carRepository.UpdateAsync(existingCar, cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> PatchAsync(Guid id, CarPatchDto patchDto, CancellationToken cancellationToken = default)
    {
        var existingCar = await _carRepository.GetByIdAsync(id, cancellationToken);
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

        await _carRepository.UpdateAsync(existingCar, cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(id, cancellationToken);

        if (car == null)
        {
            return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
        }

        await _carRepository.DeleteAsync(id, cancellationToken);

        return Result.Ok();
    }
}
