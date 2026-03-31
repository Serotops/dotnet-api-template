using System.Net.Mime;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Interfaces.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApiTemplate.Controllers;

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
    /// <returns>The car with the specified ID.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> GetById(Guid id)
    {
        var result = await _carService.GetByIdAsync(id);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retrieves all cars.
    /// </summary>
    /// <returns>A list of all cars.</returns>
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CarDto>))]
    public async Task<ActionResult<IEnumerable<CarDto>>> GetAll()
    {
        var result = await _carService.GetAllAsync();

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
    /// <returns>A paginated list of cars matching the filter criteria.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PaginationResult<CarDto>))]
    public async Task<ActionResult<PaginationResult<CarDto>>> GetFiltered([FromQuery] CarParams filterParams)
    {
        var result = await _carService.GetFilteredAsync(filterParams);

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
    /// <returns>The created car.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> Create([FromBody] CarUpsertDto dto)
    {
        var createResult = await _carService.AddAsync(dto);

        if (createResult.IsFailed)
        {
            return HandleFailure(createResult);
        }

        var id = createResult.Value;
        var getResult = await _carService.GetByIdAsync(id);

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
    /// <returns>The updated car.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> Update(Guid id, [FromBody] CarUpsertDto dto)
    {
        var updateResult = await _carService.UpdateAsync(id, dto);

        if (updateResult.IsFailed)
        {
            return HandleFailure(updateResult);
        }

        // Get the updated car to return it
        var getResult = await _carService.GetByIdAsync(id);

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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CarDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> Patch(Guid id, [FromBody] CarPatchDto patchDto)
    {
        var patchResult = await _carService.PatchAsync(id, patchDto);

        if (patchResult.IsFailed)
        {
            return HandleFailure(patchResult);
        }

        // Get the updated car to return it
        var getResult = await _carService.GetByIdAsync(id);

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
    /// <returns>No content.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _carService.DeleteAsync(id);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return NoContent();
    }

    /// <summary>
    /// Generates a text report for a car and saves it to disk.
    /// This endpoint demonstrates when to use exceptions vs Result pattern.
    /// - Business failures (car not found) use Result pattern
    /// - Infrastructure failures (disk write errors) can throw exceptions
    /// </summary>
    /// <param name="id">The unique identifier of the car.</param>
    /// <returns>The file path where the report was saved.</returns>
    [HttpPost("{id:guid}/report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> GenerateReport(Guid id)
    {
        var result = await _carService.GenerateCarReportAsync(id);

        if (result.IsFailed)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value);
    }
}
