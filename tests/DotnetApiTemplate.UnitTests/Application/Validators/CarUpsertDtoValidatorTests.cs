using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Validators;
using DotnetApiTemplate.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace DotnetApiTemplate.UnitTests.Application.Validators;

public class CarUpsertDtoValidatorTests
{
    private readonly CarUpsertDtoValidator _validator;

    public CarUpsertDtoValidatorTests()
    {
        _validator = new CarUpsertDtoValidator();
    }

    #region Make Validation Tests

    [Fact]
    public void Should_Have_Error_When_Make_Is_Empty()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Make)
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING));
    }

    [Fact]
    public void Should_Have_Error_When_Make_Is_Null()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = null!,
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

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
        var dto = new CarUpsertDto
        {
            Make = new string('A', 51), // 51 characters
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Make)
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Make_Is_Valid()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Make);
    }

    #endregion

    #region Model Validation Tests

    [Fact]
    public void Should_Have_Error_When_Model_Is_Empty()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Model)
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING));
    }

    [Fact]
    public void Should_Have_Error_When_Model_Exceeds_Maximum_Length()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = new string('A', 51),
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Model)
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));
    }

    #endregion

    #region Year Validation Tests

    [Theory]
    [InlineData(1899)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void Should_Have_Error_When_Year_Is_Before_1900(int year)
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = year,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

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
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = DateTime.Now.Year + 2,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Year)
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR));
    }

    [Theory]
    [InlineData(1901)]
    [InlineData(2000)]
    [InlineData(2020)]
    public void Should_Not_Have_Error_When_Year_Is_Valid(int year)
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = year,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Year_Is_Next_Year()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = DateTime.Now.Year + 1,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Year);
    }

    #endregion

    #region Color Validation Tests

    [Fact]
    public void Should_Have_Error_When_Color_Is_Empty()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color)
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING));
    }

    [Fact]
    public void Should_Have_Error_When_Color_Exceeds_Maximum_Length()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = new string('A', 31),
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color)
            .WithErrorCode(nameof(ErrorCode.INVALID_FIELD_LENGTH));
    }

    #endregion

    #region Price Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void Should_Have_Error_When_Price_Is_Negative(decimal price)
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = price,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorCode(nameof(ErrorCode.INVALID_PRICE));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(22000)]
    [InlineData(0.01)]
    public void Should_Not_Have_Error_When_Price_Is_Valid(decimal price)
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = price,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    #endregion

    #region VIN Validation Tests

    [Fact]
    public void Should_Have_Error_When_VIN_Exceeds_Maximum_Length()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            VIN = "123456789012345678", // 18 characters
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VIN)
            .WithErrorCode(nameof(ErrorCode.INVALID_VIN));
    }

    [Fact]
    public void Should_Not_Have_Error_When_VIN_Is_Null()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            VIN = null,
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.VIN);
    }

    [Fact]
    public void Should_Not_Have_Error_When_VIN_Is_Valid()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            VIN = "12345678901234567", // 17 characters
            Mileage = 62000
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.VIN);
    }

    #endregion

    #region Mileage Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-1000)]
    public void Should_Have_Error_When_Mileage_Is_Negative(int mileage)
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = mileage
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Mileage)
            .WithErrorCode(nameof(ErrorCode.INVALID_MILEAGE));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(62000)]
    [InlineData(200000)]
    public void Should_Not_Have_Error_When_Mileage_Is_Valid(int mileage)
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = mileage
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Mileage);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Should_Have_Multiple_Errors_When_Multiple_Fields_Invalid()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "",
            Model = "",
            Year = 0,
            Color = "",
            Price = -100,
            VIN = "123456789012345678",
            Mileage = -50
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Make);
        result.ShouldHaveValidationErrorFor(x => x.Model);
        result.ShouldHaveValidationErrorFor(x => x.Year);
        result.ShouldHaveValidationErrorFor(x => x.Color);
        result.ShouldHaveValidationErrorFor(x => x.Price);
        result.ShouldHaveValidationErrorFor(x => x.VIN);
        result.ShouldHaveValidationErrorFor(x => x.Mileage);

        result.Errors.Count.Should().BeGreaterThan(5);
    }

    [Fact]
    public void Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            VIN = "12345678901234567",
            Mileage = 62000,
            IsAvailable = true
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
