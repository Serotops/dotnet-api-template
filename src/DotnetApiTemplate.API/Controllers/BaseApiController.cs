using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Common;
using DotnetApiTemplate.Domain.Enums;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace DotnetApiTemplate.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// Inherit from this class to get access to shared methods like Result handling.
///
/// Derived controllers should add these attributes:
/// [ApiController]
/// [ApiVersion("1.0")]
/// [Route("api/v{version:apiVersion}/[controller]")]
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Helper method to handle Result failures and convert them to appropriate HTTP responses.
    /// This centralizes the logic for mapping ApplicationErrors to HTTP status codes and ApiResponse format.
    /// </summary>
    /// <param name="result">The failed Result to handle</param>
    /// <returns>An ActionResult with the appropriate HTTP status code and error details</returns>
    protected ActionResult HandleFailure(ResultBase result)
    {
        var firstError = result.Errors.FirstOrDefault();

        if (firstError is ApplicationError appError)
        {
            var errorResponse = ApiResponse<object>.ErrorResponse(
                message: appError.Message,
                errorCode: appError.ErrorCode,
                errors: result.Errors.Select(e => e.Message).ToList(),
                traceId: HttpContext.TraceIdentifier
            );

            return appError switch
            {
                NotFoundError => NotFound(errorResponse),
                ValidationFailureError => BadRequest(errorResponse),
                BusinessRuleError => BadRequest(errorResponse),
                DatabaseError => StatusCode(StatusCodes.Status500InternalServerError, errorResponse),
                _ => StatusCode(StatusCodes.Status500InternalServerError, errorResponse)
            };
        }

        // Fallback for non-ApplicationError errors
        var fallbackResponse = ApiResponse<object>.ErrorResponse(
            message: firstError?.Message ?? "An error occurred",
            errorCode: ErrorCode.UNKNOWN_ERROR,
            errors: result.Errors.Select(e => e.Message).ToList(),
            traceId: HttpContext.TraceIdentifier
        );

        return StatusCode(StatusCodes.Status500InternalServerError, fallbackResponse);
    }
}
