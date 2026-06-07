using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Application.Interfaces.Repositories;
using DotnetApiTemplate.Application.Services;
using DotnetApiTemplate.Domain.Entities;
using DotnetApiTemplate.Domain.Enums;
using FluentAssertions;
using FluentResults;
using Moq;
using Xunit;

namespace DotnetApiTemplate.UnitTests.Application.Services;

public class CarServiceTests
{
    private readonly Mock<ICarRepository> _mockRepository;
    private readonly CarService _carService;

    public CarServiceTests()
    {
        _mockRepository = new Mock<ICarRepository>();
        _carService = new CarService(_mockRepository.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_Should_Return_Success_When_Car_Exists()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var car = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(car);

        // Act
        var result = await _carService.GetByIdAsync(carId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(carId);
        result.Value.Make.Should().Be("Renault");
        result.Value.Model.Should().Be("Clio");
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var carId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _carService.GetByIdAsync(carId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        var error = result.Errors[0] as NotFoundError;
        error!.ErrorCode.Should().Be(ErrorCode.CAR_NOT_FOUND);
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Cars()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Id = Guid.NewGuid(),
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 22000,
                Mileage = 62000
            },
            new Car
            {
                Id = Guid.NewGuid(),
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            }
        };

        _mockRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(cars);

        // Act
        var result = await _carService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.First().Make.Should().Be("Renault");
        result.Value.Last().Make.Should().Be("Toyota");
        _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_List_When_No_Cars()
    {
        // Arrange
        _mockRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Car>());

        // Act
        var result = await _carService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        _mockRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetFilteredAsync Tests

    [Fact]
    public async Task GetFilteredAsync_Should_Return_Paginated_Results()
    {
        // Arrange
        var filterParams = new CarParams
        {
            PageIndex = 1,
            PageSize = 10
        };

        var cars = new List<Car>
        {
            new Car
            {
                Id = Guid.NewGuid(),
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 22000,
                Mileage = 62000
            }
        };

        var paginationResult = new PaginationResult<Car>
        {
            PageIndex = 1,
            PageSize = 10,
            TotalItems = 1,
            Data = cars
        };

        _mockRepository.Setup(x => x.GetFilteredAsync(filterParams))
            .ReturnsAsync(paginationResult);

        // Act
        var result = await _carService.GetFilteredAsync(filterParams);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageIndex.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalItems.Should().Be(1);
        result.Value.Data.Should().HaveCount(1);
        result.Value.Data[0].Make.Should().Be("Renault");
        _mockRepository.Verify(x => x.GetFilteredAsync(filterParams), Times.Once);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_Should_Add_Car_And_Return_Id()
    {
        // Arrange
        var carDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        var createdCarId = Guid.NewGuid();

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Car c, CancellationToken _) =>
            {
                c.Id = createdCarId;
                return c;
            });

        // Act
        var result = await _carService.AddAsync(carDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(createdCarId);
        _mockRepository.Verify(x => x.AddAsync(It.Is<Car>(c =>
            c.Make == "Renault" && c.Model == "Clio" && c.Year == 2020)), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_Should_Update_Car_When_Valid()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Red",
            Price = 21000,
            Mileage = 65000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);
        _mockRepository.Setup(x => x.UpdateAsync(existingCar))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _carService.UpdateAsync(carId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingCar.Color.Should().Be("Red");
        existingCar.Price.Should().Be(21000);
        existingCar.Mileage.Should().Be(65000);
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(existingCar), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _carService.UpdateAsync(carId, updateDto);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        var error = result.Errors[0] as NotFoundError;
        error!.ErrorCode.Should().Be(ErrorCode.CAR_NOT_FOUND);
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Mileage_Decreases()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 50000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);

        // Act
        var result = await _carService.UpdateAsync(carId, updateDto);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<BusinessRuleError>();
        var error = result.Errors[0] as BusinessRuleError;
        error!.ErrorCode.Should().Be(ErrorCode.INVALID_MILEAGE);
        error.Message.Should().Contain("Mileage cannot decrease");
        error.Message.Should().Contain("62000");
        error.Message.Should().Contain("50000");
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Should_Allow_Same_Mileage()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);
        _mockRepository.Setup(x => x.UpdateAsync(existingCar))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _carService.UpdateAsync(carId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(x => x.UpdateAsync(existingCar), Times.Once);
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_Should_Update_Only_Provided_Fields()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000,
            IsAvailable = true
        };

        var patchDto = new CarPatchDto
        {
            Price = 21000,
            IsAvailable = false
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);
        _mockRepository.Setup(x => x.UpdateAsync(existingCar))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _carService.PatchAsync(carId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingCar.Price.Should().Be(21000);
        existingCar.IsAvailable.Should().BeFalse();
        existingCar.Make.Should().Be("Renault");
        existingCar.Model.Should().Be("Clio");
        existingCar.Mileage.Should().Be(62000);
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(existingCar), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var patchDto = new CarPatchDto
        {
            Price = 21000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _carService.PatchAsync(carId, patchDto);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        var error = result.Errors[0] as NotFoundError;
        error!.ErrorCode.Should().Be(ErrorCode.CAR_NOT_FOUND);
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_Should_Fail_When_Mileage_Decreases()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        var patchDto = new CarPatchDto
        {
            Mileage = 50000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);

        // Act
        var result = await _carService.PatchAsync(carId, patchDto);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<BusinessRuleError>();
        var error = result.Errors[0] as BusinessRuleError;
        error!.ErrorCode.Should().Be(ErrorCode.INVALID_MILEAGE);
        error.Message.Should().Contain("Mileage cannot decrease");
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Car>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_Should_Update_All_Fields_When_All_Provided()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            VIN = "12345678901234567",
            Mileage = 62000,
            IsAvailable = true
        };

        var patchDto = new CarPatchDto
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2021,
            Color = "White",
            Price = 25000,
            VIN = "98765432109876543",
            Mileage = 65000,
            IsAvailable = false
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);
        _mockRepository.Setup(x => x.UpdateAsync(existingCar))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _carService.PatchAsync(carId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingCar.Make.Should().Be("Toyota");
        existingCar.Model.Should().Be("Corolla");
        existingCar.Year.Should().Be(2021);
        existingCar.Color.Should().Be("White");
        existingCar.Price.Should().Be(25000);
        existingCar.VIN.Should().Be("98765432109876543");
        existingCar.Mileage.Should().Be(65000);
        existingCar.IsAvailable.Should().BeFalse();
        _mockRepository.Verify(x => x.UpdateAsync(existingCar), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_Should_Allow_Null_Mileage_Without_Validation()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var existingCar = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        var patchDto = new CarPatchDto
        {
            Price = 21000,
            Mileage = null
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(existingCar);
        _mockRepository.Setup(x => x.UpdateAsync(existingCar))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _carService.PatchAsync(carId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingCar.Mileage.Should().Be(62000);
        existingCar.Price.Should().Be(21000);
        _mockRepository.Verify(x => x.UpdateAsync(existingCar), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_Should_Delete_Car_When_Exists()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var car = new Car
        {
            Id = carId,
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync(car);
        _mockRepository.Setup(x => x.DeleteAsync(carId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _carService.DeleteAsync(carId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(carId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var carId = Guid.NewGuid();
        _mockRepository.Setup(x => x.GetByIdAsync(carId))
            .ReturnsAsync((Car?)null);

        // Act
        var result = await _carService.DeleteAsync(carId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<NotFoundError>();
        var error = result.Errors[0] as NotFoundError;
        error!.ErrorCode.Should().Be(ErrorCode.CAR_NOT_FOUND);
        _mockRepository.Verify(x => x.GetByIdAsync(carId), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion
}
