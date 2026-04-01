using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Validators;
using DotnetApiTemplate.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DotnetApiTemplate.UnitTests.Application.Validators;

public class CarPatchDtoValidatorTests
{
    private readonly CarPatchDtoValidator _validator;

    public CarPatchDtoValidatorTests()
    {
        _validator = new CarPatchDtoValidator();
    }

    #region Make Validation Tests

    [Fact]
    public void Should_Have_Error_When_Make_Is_Empty_String()
    {
        // Arrange
        var dto = new CarPatchDto { Make = "" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Make)
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING));
    }

    [Fact]
    public void Should_Have_Error_When_Make_Exceeds_Maximum_Length()
    {
        // Arrange
        var dto = new CarPatchDto { Make = new string('A', 51) };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Make)
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));
    }

    [Fact]
    public void Should_Not_Validate_Make_When_Null()
    {
        // Arrange
        var dto = new CarPatchDto { Make = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Make);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Make_Is_Valid()
    {
        // Arrange
        var dto = new CarPatchDto { Make = "Renault" };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Make);
    }

    #endregion

    #region Year Validation Tests

    [Theory]
    [InlineData(1899)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Have_Error_When_Year_Is_Before_1900(int year)
    {
        // Arrange
        var dto = new CarPatchDto { Year = year };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year)
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR));
    }

    [Fact]
    public void Should_Have_Error_When_Year_Is_More_Than_Next_Year()
    {
        // Arrange
        var dto = new CarPatchDto { Year = DateTime.Now.Year + 2 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year)
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR));
    }

    [Fact]
    public void Should_Not_Validate_Year_When_Null()
    {
        // Arrange
        var dto = new CarPatchDto { Year = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Theory]
    [InlineData(1901)]
    [InlineData(2020)]
    public void Should_Not_Have_Error_When_Year_Is_Valid(int year)
    {
        // Arrange
        var dto = new CarPatchDto { Year = year };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    #endregion

    #region Price Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_Price_Is_Negative(decimal price)
    {
        // Arrange
        var dto = new CarPatchDto { Price = price };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorCode(nameof(ErrorCode.INVALID_PRICE));
    }

    [Fact]
    public void Should_Not_Validate_Price_When_Null()
    {
        // Arrange
        var dto = new CarPatchDto { Price = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(22000)]
    public void Should_Not_Have_Error_When_Price_Is_Valid(decimal price)
    {
        // Arrange
        var dto = new CarPatchDto { Price = price };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    #endregion

    #region Mileage Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Have_Error_When_Mileage_Is_Negative(int mileage)
    {
        // Arrange
        var dto = new CarPatchDto { Mileage = mileage };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Mileage)
            .WithErrorCode(nameof(ErrorCode.INVALID_MILEAGE));
    }

    [Fact]
    public void Should_Not_Validate_Mileage_When_Null()
    {
        // Arrange
        var dto = new CarPatchDto { Mileage = null };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Mileage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(62000)]
    public void Should_Not_Have_Error_When_Mileage_Is_Valid(int mileage)
    {
        // Arrange
        var dto = new CarPatchDto { Mileage = mileage };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Mileage);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Should_Pass_When_All_Fields_Are_Null()
    {
        // Arrange
        var dto = new CarPatchDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Only_One_Field_Is_Provided()
    {
        // Arrange
        var dto = new CarPatchDto { Price = 20000 };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Errors_Only_For_Invalid_Provided_Fields()
    {
        // Arrange
        var dto = new CarPatchDto
        {
            Make = "", // Invalid
            Year = 0, // Invalid
            Price = 22000, // Valid
            Mileage = null // Not validated
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Make);
        result.ShouldHaveValidationErrorFor(x => x.Year);
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
        result.ShouldNotHaveValidationErrorFor(x => x.Mileage);
    }

    #endregion
}
