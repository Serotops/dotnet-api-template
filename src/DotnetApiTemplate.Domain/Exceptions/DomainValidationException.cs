using DotnetApiTemplate.Domain.Enums;

namespace DotnetApiTemplate.Domain.Exceptions;

public class DomainValidationException : DotnetApiTemplateException
{
    public DomainValidationException(string message, ErrorCode errorCode = ErrorCode.VALIDATION_ERROR)
        : base(message, errorCode)
    {
    }
}
