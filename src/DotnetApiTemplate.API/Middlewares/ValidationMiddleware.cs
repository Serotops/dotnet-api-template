using System.Text.Json;
using DotnetApiTemplate.Common;
using DotnetApiTemplate.Domain.Enums;
using FluentValidation;

namespace DotnetApiTemplate.Middlewares;

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var validationErrors = exception.Errors
            .Select(error => new ValidationError
            {
                Field = error.PropertyName,
                Message = error.ErrorMessage,
                AttemptedValue = error.AttemptedValue
            })
            .ToList();

        var generalErrors = exception.Errors
            .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
            .ToList();

        var response = ApiResponse<object>.ErrorResponse(
            message: "One or more validation errors occurred",
            errorCode: ErrorCode.VALIDATION_ERROR,
            errors: generalErrors,
            validationErrors: validationErrors,
            traceId: context.TraceIdentifier
        );

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}
