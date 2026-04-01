using DotnetApiTemplate.Domain.Enums;

namespace DotnetApiTemplate.Domain.Exceptions;

public class ResourceNotFoundException : DotnetApiTemplateException
{
    public ResourceNotFoundException(string message, ErrorCode errorCode = ErrorCode.RESOURCE_NOT_FOUND)
        : base(message, errorCode)
    {
    }
}
