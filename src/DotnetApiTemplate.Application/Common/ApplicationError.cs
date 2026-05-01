using DotnetApiTemplate.Domain.Enums;
using FluentResults;

namespace DotnetApiTemplate.Application.Common;

/// <summary>
/// Base class for application-level errors that integrate with FluentResults
/// </summary>
public class ApplicationError : Error
{
    public ErrorCode ErrorCode { get; }

    public ApplicationError(string message, ErrorCode errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Error for resource not found scenarios
/// </summary>
public class NotFoundError : ApplicationError
{
    public NotFoundError(string message, ErrorCode errorCode = ErrorCode.RESOURCE_NOT_FOUND)
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Error for validation failures
/// </summary>
public class ValidationFailureError : ApplicationError
{
    public ValidationFailureError(string message, ErrorCode errorCode = ErrorCode.VALIDATION_ERROR)
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Error for business rule violations
/// </summary>
public class BusinessRuleError : ApplicationError
{
    public BusinessRuleError(string message, ErrorCode errorCode)
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Error for database/persistence failures
/// </summary>
public class DatabaseError : ApplicationError
{
    public DatabaseError(string message, ErrorCode errorCode = ErrorCode.DATABASE_ERROR)
        : base(message, errorCode)
    {
    }
}

/// <summary>
/// Error for file system / IO failures
/// </summary>
public class IoError : ApplicationError
{
    public IoError(string message, ErrorCode errorCode = ErrorCode.IO_ERROR)
        : base(message, errorCode)
    {
    }
}
