using System.Net;
using System.Text.Json;
using DotnetApiTemplate.Common;
using DotnetApiTemplate.Domain.Enums;
using FluentValidation;

namespace DotnetApiTemplate.Middlewares;

/// <summary>
/// Middleware to handle UNEXPECTED exceptions that were not caught by the Result pattern.
///
/// With the Result pattern in place, this middleware now focuses on:
/// - FluentValidation exceptions (from [ApiController] model validation)
/// - Infrastructure failures (database connection errors, file system errors)
/// - Unexpected runtime errors (NullReferenceException, OutOfMemoryException, etc.)
/// - External service failures (HTTP timeouts, network errors)
///
/// Expected business failures (not found, validation errors, business rule violations)
/// are now handled via Result pattern in the service/controller layer.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Don't handle TaskCanceledException or OperationCanceledException
            // These occur when the client disconnects or cancels the request, which is normal
            if (ex is TaskCanceledException or OperationCanceledException)
            {
                // Just return without handling - the response will naturally fail
                return;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        _logger.LogError(exception, "An unexpected error occurred: {ExceptionType} - {Message}, CorrelationId: {CorrelationId}",
            exception.GetType().Name, exception.Message, correlationId);

        HttpStatusCode statusCode;
        string message;
        ErrorCode errorCode;
        List<string> errors;
        List<ValidationError>? validationErrors = null;

        switch (exception)
        {
            // FluentValidation exceptions (from [ApiController] automatic model validation)
            case ValidationException validationEx:
                statusCode = HttpStatusCode.BadRequest;
                message = "One or more validation errors occurred";
                errorCode = ErrorCode.VALIDATION_ERROR;
                errors = validationEx.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToList();
                validationErrors = validationEx.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Message = error.ErrorMessage,
                    ErrorCode = error.ErrorCode,  // Map FluentValidation ErrorCode to our ValidationError
                    AttemptedValue = error.AttemptedValue
                }).ToList();
                break;

            // Database/EF Core exceptions
            case Microsoft.EntityFrameworkCore.DbUpdateException dbEx:
                statusCode = HttpStatusCode.InternalServerError;
                message = "A database error occurred while processing your request";
                errorCode = ErrorCode.DATABASE_ERROR;
                errors = new List<string> { "A database error occurred" };
                _logger.LogError(dbEx, "Database update error: {InnerMessage}", dbEx.InnerException?.Message);
                break;

            // IO/File system exceptions (like in GenerateCarReportAsync)
            case IOException ioEx:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An error occurred while accessing the file system";
                errorCode = ErrorCode.INTERNAL_SERVER_ERROR;
                errors = new List<string> { "File system error" };
                break;

            case UnauthorizedAccessException uaEx:
                statusCode = HttpStatusCode.InternalServerError;
                message = "Access denied to a system resource";
                errorCode = ErrorCode.INTERNAL_SERVER_ERROR;
                errors = new List<string> { "Access denied" };
                break;

            // HTTP/Network exceptions
            case HttpRequestException httpEx:
                statusCode = HttpStatusCode.BadGateway;
                message = "An error occurred while communicating with an external service";
                errorCode = ErrorCode.INTERNAL_SERVER_ERROR;
                errors = new List<string> { "External service error" };
                break;

            case TimeoutException:
                statusCode = HttpStatusCode.RequestTimeout;
                message = "The request timed out";
                errorCode = ErrorCode.INTERNAL_SERVER_ERROR;
                errors = new List<string> { "Request timeout" };
                break;

            // All other unexpected exceptions
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred. Please try again later.";
                errorCode = ErrorCode.INTERNAL_SERVER_ERROR;
                errors = new List<string> { "An unexpected error occurred. Please try again later." };
                break;
        }

        var response = ApiResponse<object>.ErrorResponse(
            message: message,
            errorCode: errorCode,
            errors: errors,
            validationErrors: validationErrors,
            traceId: correlationId
        );

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}
