using DotnetApiTemplate.Application.Common.Params;
using DotnetApiTemplate.Domain.Entities;
using DotnetApiTemplate.IntegrationTests.Base;
using DotnetApiTemplate.IntegrationTests.Fixtures;
using DotnetApiTemplate.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace DotnetApiTemplate.IntegrationTests.Repositories;

public class CarRepositoryTests : IntegrationTestBase
{
    private readonly CarRepository _repository;

    public CarRepositoryTests(ApiTestFactory factory) : base(factory)
    {
        _repository = new CarRepository(DbContext);
        ClearDatabase();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_Should_Return_Car_When_Exists()
    {
        // Arrange
        var car = new Car
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        // Act
        var result = await _repository.GetByIdAsync(car.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(car.Id);
        result.Make.Should().Be("Renault");
        result.Model.Should().Be("Clio");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
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
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 22000,
                Mileage = 62000
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            },
            new Car
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                Color = "Blue",
                Price = 28000,
                Mileage = 15000
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Select(c => c.Make).Should().Contain(new[] { "Renault", "Toyota", "Honda" });
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_Empty_When_No_Cars()
    {
        // Arrange
        // Database is already cleared in constructor

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_Should_Add_Car_And_Generate_Id()
    {
        // Arrange
        var car = new Car
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var result = await _repository.AddAsync(car);
        await SaveAndClearTracking();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Make.Should().Be("Renault");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it's in the database
        var dbCar = await _repository.GetByIdAsync(result.Id);
        dbCar.Should().NotBeNull();
        dbCar!.Make.Should().Be("Renault");
    }

    [Fact]
    public async Task AddAsync_Should_Set_Audit_Properties()
    {
        // Arrange
        var car = new Car
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2021,
            Color = "White",
            Price = 25000,
            Mileage = 30000
        };

        // Act
        var result = await _repository.AddAsync(car);
        await SaveAndClearTracking();

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.ModifiedAt.Should().BeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_Should_Update_Car_Properties()
    {
        // Arrange
        var car = new Car
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        // Modify the car
        car.Color = "Red";
        car.Price = 21000;
        car.Mileage = 65000;

        // Act
        await _repository.UpdateAsync(car);
        await SaveAndClearTracking();

        // Assert
        var updated = await _repository.GetByIdAsync(car.Id);
        updated.Should().NotBeNull();
        updated!.Color.Should().Be("Red");
        updated.Price.Should().Be(21000);
        updated.Mileage.Should().Be(65000);
        updated.ModifiedAt.Should().NotBeNull();
        updated.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_Should_Set_ModifiedAt()
    {
        // Arrange
        var car = new Car
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        var originalModifiedAt = car.ModifiedAt;

        // Modify the car
        car.Price = 21000;

        // Act
        await _repository.UpdateAsync(car);
        await SaveAndClearTracking();

        // Assert
        var updated = await _repository.GetByIdAsync(car.Id);
        updated!.ModifiedAt.Should().NotBe(originalModifiedAt);
        updated.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_Should_Remove_Car()
    {
        // Arrange
        var car = new Car
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        // Act
        await _repository.DeleteAsync(car.Id);
        await SaveAndClearTracking();

        // Assert
        var deleted = await _repository.GetByIdAsync(car.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Should_Not_Throw_When_Car_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var act = async () => await _repository.DeleteAsync(nonExistentId);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetFilteredAsync Tests

    [Fact]
    public async Task GetFilteredAsync_Should_Return_Paginated_Results()
    {
        // Arrange
        var cars = Enumerable.Range(1, 25).Select(i => new Car
        {
            Make = $"Make{i}",
            Model = $"Model{i}",
            Year = 2020 + (i % 3),
            Color = i % 2 == 0 ? "Black" : "White",
            Price = 20000 + i * 1000,
            Mileage = 50000 + i * 1000
        }).ToList();

        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(25);
        result.Data.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Filter_By_Make()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 22000,
                Mileage = 62000
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            },
            new Car
            {
                Make = "Renault",
                Model = "Megane",
                Year = 2021,
                Color = "Blue",
                Price = 28000,
                Mileage = 40000
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            Make = "Renault",
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c => c.Make == "Renault");
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Filter_By_MinYear()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2018,
                Color = "Black",
                Price = 18000,
                Mileage = 80000
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            },
            new Car
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                Color = "Blue",
                Price = 28000,
                Mileage = 15000
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            MinYear = 2020,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c => c.Year >= 2020);
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Filter_By_MaxYear()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2018,
                Color = "Black",
                Price = 18000,
                Mileage = 80000
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            },
            new Car
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                Color = "Blue",
                Price = 28000,
                Mileage = 15000
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            MaxYear = 2021,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c => c.Year <= 2021);
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Filter_By_MinPrice()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 18000,
                Mileage = 62000
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            },
            new Car
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                Color = "Blue",
                Price = 28000,
                Mileage = 15000
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            MinPrice = 24000,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c => c.Price >= 24000);
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Filter_By_MaxPrice()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 18000,
                Mileage = 62000
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000
            },
            new Car
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                Color = "Blue",
                Price = 28000,
                Mileage = 15000
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            MaxPrice = 25000,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c => c.Price <= 25000);
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Filter_By_IsAvailable()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 22000,
                Mileage = 62000,
                IsAvailable = true
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000,
                IsAvailable = false
            },
            new Car
            {
                Make = "Honda",
                Model = "Civic",
                Year = 2022,
                Color = "Blue",
                Price = 28000,
                Mileage = 15000,
                IsAvailable = true
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            IsAvailable = true,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c => c.IsAvailable);
    }

    [Fact]
    public async Task GetFilteredAsync_Should_Apply_Multiple_Filters()
    {
        // Arrange
        var cars = new List<Car>
        {
            new Car
            {
                Make = "Renault",
                Model = "Clio",
                Year = 2020,
                Color = "Black",
                Price = 22000,
                Mileage = 62000,
                IsAvailable = true
            },
            new Car
            {
                Make = "Renault",
                Model = "Megane",
                Year = 2018,
                Color = "White",
                Price = 18000,
                Mileage = 80000,
                IsAvailable = true
            },
            new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2021,
                Color = "White",
                Price = 25000,
                Mileage = 30000,
                IsAvailable = false
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        var filterParams = new CarParams
        {
            Make = "Renault",
            MinYear = 2019,
            IsAvailable = true,
            PageIndex = 1,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetFilteredAsync(filterParams);

        // Assert
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Data.Should().HaveCount(1);
        result.Data[0].Make.Should().Be("Renault");
        result.Data[0].Year.Should().BeGreaterThanOrEqualTo(2019);
        result.Data[0].IsAvailable.Should().BeTrue();
    }

    #endregion
}
