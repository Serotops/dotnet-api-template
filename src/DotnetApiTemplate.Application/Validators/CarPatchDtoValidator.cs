using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Domain.Enums;
using FluentValidation;

namespace DotnetApiTemplate.Application.Validators;

/// <summary>
/// Validator for CarPatchDto.
/// Only validates properties that are provided (not null).
/// Business rules (like mileage cannot decrease) are handled in the service layer.
/// </summary>
public class CarPatchDtoValidator : AbstractValidator<CarPatchDto>
{
    public CarPatchDtoValidator()
    {
        // Only validate Make if it's provided
        RuleFor(x => x.Make)
            .NotEmpty()
            .WithMessage("Make cannot be empty.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(50)
            .WithMessage("Make must be 50 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH))
            .When(x => x.Make != null);

        // Only validate Model if it's provided
        RuleFor(x => x.Model)
            .NotEmpty()
            .WithMessage("Model cannot be empty.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(50)
            .WithMessage("Model must be 50 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH))
            .When(x => x.Model != null);

        // Only validate Year if it's provided
        RuleFor(x => x.Year)
            .GreaterThan(1900)
            .WithMessage("Year must be after 1900.")
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR))
            .LessThanOrEqualTo(DateTime.Now.Year + 1)
            .WithMessage("Year cannot be more than one year in the future.")
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR))
            .When(x => x.Year.HasValue);

        // Only validate Color if it's provided
        RuleFor(x => x.Color)
            .NotEmpty()
            .WithMessage("Color cannot be empty.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(30)
            .WithMessage("Color must be 30 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH))
            .When(x => x.Color != null);

        // Only validate Price if it's provided
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price must be a positive value.")
            .WithErrorCode(nameof(ErrorCode.INVALID_PRICE))
            .When(x => x.Price.HasValue);

        // Only validate VIN if it's provided
        RuleFor(x => x.VIN)
            .NotEmpty()
            .WithMessage("VIN cannot be empty.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(17)
            .WithMessage("VIN must be 17 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_VIN))
            .When(x => x.VIN != null);

        // Only validate Mileage if it's provided
        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Mileage must be a positive value.")
            .WithErrorCode(nameof(ErrorCode.INVALID_MILEAGE))
            .When(x => x.Mileage.HasValue);
    }
}
