using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Domain.Enums;
using FluentValidation;

namespace DotnetApiTemplate.Application.Validators;

public class CarUpsertDtoValidator : AbstractValidator<CarUpsertDto>
{
    public CarUpsertDtoValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty()
            .WithMessage("Make is required.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(50)
            .WithMessage("Make must be 50 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));

        RuleFor(x => x.Model)
            .NotEmpty()
            .WithMessage("Model is required.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(50)
            .WithMessage("Model must be 50 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));

        RuleFor(x => x.Year)
            .GreaterThan(1900)
            .WithMessage("Year must be after 1900.")
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR))
            .LessThanOrEqualTo(DateTime.Now.Year + 1)
            .WithMessage("Year cannot be more than one year in the future.")
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR));

        RuleFor(x => x.Color)
            .NotEmpty()
            .WithMessage("Color is required.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING))
            .MaximumLength(30)
            .WithMessage("Color must be 30 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price must be a positive value.")
            .WithErrorCode(nameof(ErrorCode.INVALID_PRICE));

        RuleFor(x => x.VIN)
            .MaximumLength(17)
            .WithMessage("VIN must be 17 characters or less.")
            .WithErrorCode(nameof(ErrorCode.INVALID_VIN))
            .When(x => !string.IsNullOrEmpty(x.VIN));

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Mileage must be a positive value.")
            .WithErrorCode(nameof(ErrorCode.INVALID_MILEAGE));
    }
}
