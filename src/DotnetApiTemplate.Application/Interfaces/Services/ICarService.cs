using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using FluentResults;

namespace DotnetApiTemplate.Application.Interfaces.Services;

public interface ICarService
{
    Task<Result<CarDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<CarDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PaginationResult<CarDto>>> GetFilteredAsync(CarParams filterParams, CancellationToken cancellationToken = default);
    Task<Result<Guid>> AddAsync(CarUpsertDto car, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Guid id, CarUpsertDto car, CancellationToken cancellationToken = default);
    Task<Result> PatchAsync(Guid id, CarPatchDto patchDto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
