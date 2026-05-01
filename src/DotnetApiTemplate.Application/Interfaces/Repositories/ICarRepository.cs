using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Domain.Entities;

namespace DotnetApiTemplate.Application.Interfaces.Repositories;

/// <summary>
/// Car repository interface.
/// Inherits common CRUD operations from IRepository and adds Car-specific methods.
/// </summary>
public interface ICarRepository : IRepository<Car>
{
    Task<PaginationResult<Car>> GetFilteredAsync(CarParams filterParams, CancellationToken cancellationToken = default);
}
