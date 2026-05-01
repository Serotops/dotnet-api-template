using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetApiTemplate.Persistence.Repositories;

/// <summary>
/// Car repository implementation.
/// Inherits common CRUD operations from Repository and implements Car-specific methods.
/// </summary>
public class CarRepository(DotnetApiTemplateDbContext context)
    : Repository<Car>(context), ICarRepository
{
    public async Task<PaginationResult<Car>> GetFilteredAsync(CarParams filterParams, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(filterParams.Make))
            query = query.Where(c => c.Make == filterParams.Make);

        if (!string.IsNullOrEmpty(filterParams.Model))
            query = query.Where(c => c.Model == filterParams.Model);

        if (filterParams.MinYear.HasValue)
            query = query.Where(c => c.Year >= filterParams.MinYear.Value);

        if (filterParams.MaxYear.HasValue)
            query = query.Where(c => c.Year <= filterParams.MaxYear.Value);

        if (filterParams.MinPrice.HasValue)
            query = query.Where(c => c.Price >= filterParams.MinPrice.Value);

        if (filterParams.MaxPrice.HasValue)
            query = query.Where(c => c.Price <= filterParams.MaxPrice.Value);

        if (filterParams.IsAvailable.HasValue)
            query = query.Where(c => c.IsAvailable == filterParams.IsAvailable.Value);

        query = filterParams.Sort switch
        {
            "makeAsc" => query.OrderBy(c => c.Make),
            "makeDesc" => query.OrderByDescending(c => c.Make),
            "modelAsc" => query.OrderBy(c => c.Model),
            "modelDesc" => query.OrderByDescending(c => c.Model),
            "yearAsc" => query.OrderBy(c => c.Year),
            "yearDesc" => query.OrderByDescending(c => c.Year),
            "priceAsc" => query.OrderBy(c => c.Price),
            "priceDesc" => query.OrderByDescending(c => c.Price),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var totalItems = await query.CountAsync(cancellationToken);

        var skip = (filterParams.PageIndex - 1) * filterParams.PageSize;
        var data = await query.Skip(skip).Take(filterParams.PageSize).ToListAsync(cancellationToken);

        return new PaginationResult<Car>
        {
            PageIndex = filterParams.PageIndex,
            PageSize = filterParams.PageSize,
            TotalItems = totalItems,
            Data = data
        };
    }
}
