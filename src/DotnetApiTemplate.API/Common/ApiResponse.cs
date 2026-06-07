using DotnetApiTemplate.Domain.Enums;

namespace DotnetApiTemplate.API.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public List<string>? Errors { get; set; }
    public List<ValidationError>? ValidationErrors { get; set; }
    public string? TraceId { get; set; }

    public static ApiResponse<T> ErrorResponse(
        string message,
        ErrorCode errorCode,
        List<string>? errors = null,
        List<ValidationError>? validationErrors = null,
        string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode.ToString(),
            Errors = errors,
            ValidationErrors = validationErrors,
            TraceId = traceId
        };
    }
}
