using DotnetApiTemplate.Domain.Enums;

namespace DotnetApiTemplate.Domain.Exceptions;

public class DotnetApiTemplateException : Exception
{
    public ErrorCode ErrorCode { get; }

    public DotnetApiTemplateException(string message, ErrorCode errorCode = ErrorCode.UNKNOWN_ERROR) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DotnetApiTemplateException(string message, ErrorCode errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
