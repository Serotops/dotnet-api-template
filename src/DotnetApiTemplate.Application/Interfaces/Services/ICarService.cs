using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using FluentResults;

namespace DotnetApiTemplate.Application.Interfaces.Services;

public interface ICarService
{
    Task<Result<CarDto>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<CarDto>>> GetAllAsync();
    Task<Result<PaginationResult<CarDto>>> GetFilteredAsync(CarParams filterParams);
    Task<Result<Guid>> AddAsync(CarUpsertDto car);
    Task<Result> UpdateAsync(Guid id, CarUpsertDto car);
    Task<Result> PatchAsync(Guid id, CarPatchDto patchDto);
    Task<Result> DeleteAsync(Guid id);

    // Example method that demonstrates when exceptions should still be used (for truly unexpected errors)
    Task<Result<string>> GenerateCarReportAsync(Guid id);
}
