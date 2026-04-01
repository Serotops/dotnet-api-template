namespace DotnetApiTemplate.Common;

/// <summary>
/// Represents a field-level validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// The field that failed validation (e.g., "Make", "Year")
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// The error message for this field (in English, for frontend translation)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Specific error code for frontend translation (e.g., "INVALID_YEAR", "INVALID_PRICE")
    /// Maps to ErrorCode enum values for consistent error handling
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// The attempted value that failed validation (optional)
    /// </summary>
    public object? AttemptedValue { get; set; }
}
