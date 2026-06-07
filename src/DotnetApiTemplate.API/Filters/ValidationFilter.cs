using DotnetApiTemplate.API.Common;
using DotnetApiTemplate.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DotnetApiTemplate.API.Filters;

/// <summary>
/// Custom validation filter to handle FluentValidation errors and return ApiResponse format.
/// This filter runs before controller actions and validates the model manually to access ErrorCodes.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // First check if there are any model binding errors
        if (!context.ModelState.IsValid)
        {
            var modelErrors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => $"{x.Key}: {e.ErrorMessage}"))
                .ToList();

            var validationErrors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new ValidationError
                {
                    Field = x.Key,
                    Message = e.ErrorMessage,
                    ErrorCode = nameof(ErrorCode.INVALID_FIELD_FORMAT)
                }))
                .ToList();

            var response = ApiResponse<object>.ErrorResponse(
                message: "Invalid request format",
                errorCode: ErrorCode.BAD_REQUEST,
                errors: modelErrors,
                validationErrors: validationErrors,
                traceId: context.HttpContext.TraceIdentifier
            );

            context.Result = new BadRequestObjectResult(response);
            return;
        }

        // Get all action parameters that need validation
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument))
                continue;

            // If argument is null but parameter is required, return error
            if (argument == null)
            {
                // Check if the parameter is nullable
                var parameterType = parameter.ParameterType;
                var isNullable = !parameterType.IsValueType ||
                                 Nullable.GetUnderlyingType(parameterType) != null;

                if (!isNullable)
                {
                    var response = ApiResponse<object>.ErrorResponse(
                        message: $"The {parameter.Name} field is required",
                        errorCode: ErrorCode.REQUIRED_FIELD_MISSING,
                        errors: new List<string> { $"{parameter.Name}: The {parameter.Name} field is required" },
                        validationErrors: new List<ValidationError>
                        {
                            new ValidationError
                            {
                                Field = parameter.Name,
                                Message = $"The {parameter.Name} field is required",
                                ErrorCode = nameof(ErrorCode.REQUIRED_FIELD_MISSING)
                            }
                        },
                        traceId: context.HttpContext.TraceIdentifier
                    );

                    context.Result = new BadRequestObjectResult(response);
                    return;
                }

                continue;
            }

            var argumentType = argument.GetType();

            // Try to get a validator for this type
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator == null)
                continue;

            // Validate using FluentValidation
            var validationContext = new ValidationContext<object>(argument);
            var validationResult = await validator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                // Build validation errors with ErrorCodes
                var validationErrors = validationResult.Errors.Select(error => new ValidationError
                {
                    Field = error.PropertyName,
                    Message = error.ErrorMessage,
                    ErrorCode = error.ErrorCode,  // FluentValidation ErrorCode is available here!
                    AttemptedValue = error.AttemptedValue
                }).ToList();

                var errors = validationResult.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToList();

                var response = ApiResponse<object>.ErrorResponse(
                    message: "One or more validation errors occurred",
                    errorCode: ErrorCode.VALIDATION_ERROR,
                    errors: errors,
                    validationErrors: validationErrors,
                    traceId: context.HttpContext.TraceIdentifier
                );

                context.Result = new BadRequestObjectResult(response);
                return;
            }
        }

        await next();
    }
}
