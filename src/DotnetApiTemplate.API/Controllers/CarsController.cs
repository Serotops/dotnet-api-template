using System.Net.Mime;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Interfaces.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApiTemplate.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class CarsController(ICarService carService) : BaseApiController
{
    private readonly ICarService _carService = carService;

    /// <summary>
    /// Retrieves a car by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the car.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The car with the specified ID.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _carService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves all cars.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all cars.</returns>
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CarDto>))]
    public async Task<ActionResult<IEnumerable<CarDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _carService.GetAllAsync(cancellationToken);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves cars with pagination and filtering.
    /// </summary>
    /// <param name="filterParams">Pagination and filter parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of cars matching the filter criteria.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginationResult<CarDto>))]
    public async Task<ActionResult<PaginationResult<CarDto>>> GetFiltered([FromQuery] CarParams filterParams, CancellationToken cancellationToken)
    {
        var result = await _carService.GetFilteredAsync(filterParams, cancellationToken);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new car.
    /// </summary>
    /// <param name="dto">The car data to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created car.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CarDto>> Create([FromBody] CarUpsertDto dto, CancellationToken cancellationToken)
    {
        var createResult = await _carService.AddAsync(dto, cancellationToken);

        if (createResult.IsFailed)
        {
            return HandleFailure(createResult);
        }

        var id = createResult.Value;
        var getResult = await _carService.GetByIdAsync(id, cancellationToken);

        if (getResult.IsFailed)
        {
            return HandleFailure(getResult);
        }

        return CreatedAtAction(nameof(GetById), new { id }, getResult.Value);
    }

    /// <summary>
    /// Updates an existing car.
    /// </summary>
    /// <param name="id">The unique identifier of the car to update.</param>
    /// <param name="dto">The updated car data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated car.</returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> Update(Guid id, [FromBody] CarUpsertDto dto, CancellationToken cancellationToken)
    {
        var updateResult = await _carService.UpdateAsync(id, dto, cancellationToken);

        if (updateResult.IsFailed)
        {
            return HandleFailure(updateResult);
        }

        // Get the updated car to return it
        var getResult = await _carService.GetByIdAsync(id, cancellationToken);

        if (getResult.IsFailed)
        {
            return HandleFailure(getResult);
        }

        return Ok(getResult.Value);
    }

    /// <summary>
    /// Partially updates an existing car.
    /// Only the properties provided in the request body will be updated.
    /// </summary>
    /// <param name="id">The unique identifier of the car to update.</param>
    /// <param name="patchDto">The properties to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated car.</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/v1/cars/{id}
    ///     {
    ///         "price": 20000,
    ///         "isAvailable": false
    ///     }
    ///
    /// You can update any combination of properties. Omitted properties will not be changed.
    /// </remarks>
    [HttpPatch("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> Patch(Guid id, [FromBody] CarPatchDto patchDto, CancellationToken cancellationToken)
    {
        var patchResult = await _carService.PatchAsync(id, patchDto, cancellationToken);

        if (patchResult.IsFailed)
        {
            return HandleFailure(patchResult);
        }

        // Get the updated car to return it
        var getResult = await _carService.GetByIdAsync(id, cancellationToken);

        if (getResult.IsFailed)
        {
            return HandleFailure(getResult);
        }

        return Ok(getResult.Value);
    }

    /// <summary>
    /// Deletes a car.
    /// </summary>
    /// <param name="id">The unique identifier of the car to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _carService.DeleteAsync(id, cancellationToken);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return NoContent();
    }

}
