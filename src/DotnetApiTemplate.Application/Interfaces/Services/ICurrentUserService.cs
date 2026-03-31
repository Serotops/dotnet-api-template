namespace DotnetApiTemplate.Application.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? GetUserId();
}
