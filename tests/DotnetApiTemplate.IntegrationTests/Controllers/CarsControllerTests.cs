using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.API.Common;
using DotnetApiTemplate.Domain.Entities;
using DotnetApiTemplate.IntegrationTests.Base;
using DotnetApiTemplate.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DotnetApiTemplate.IntegrationTests.Controllers;

public class CarsControllerTests : IntegrationTestBase
{
    private const string BaseUrl = "/api/v1/cars";

    public CarsControllerTests(ApiTestFactory factory) : base(factory)
    {
        ClearDatabase();
    }

    #region POST Tests

    [Fact]
    public async Task Create_Should_Return_Created_With_Valid_Data()
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
        var response = await PostAsJsonAsync(BaseUrl, dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var car = await DeserializeResponse<CarDto>(response);
        car.Should().NotBeNull();
        car!.Make.Should().Be("Renault");
        car.Model.Should().Be("Clio");
        car.Year.Should().Be(2020);
        car.Id.Should().NotBeEmpty();

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(car.Id.ToString());
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Make_Is_Empty()
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
        var response = await PostAsJsonAsync(BaseUrl, dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ValidationErrors.Should().NotBeNull();
        apiResponse.ValidationErrors.Should().ContainSingle(e => e.Field == "Make");
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Multiple_Fields_Invalid()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "",
            Model = "",
            Year = 1800,
            Color = "",
            Price = -100,
            Mileage = -50
        };

        // Act
        var response = await PostAsJsonAsync(BaseUrl, dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.ValidationErrors.Should().NotBeNull();
        apiResponse.ValidationErrors.Should().HaveCountGreaterThan(4);
    }

    #endregion

    #region GET Tests

    [Fact]
    public async Task GetById_Should_Return_Car_When_Exists()
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
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/{car.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<CarDto>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(car.Id);
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Corolla");
    }

    [Fact]
    public async Task GetById_Should_Return_NotFound_When_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_Should_Return_All_Cars()
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
            }
        };
        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<List<CarDto>>(response);
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFiltered_Should_Return_Paginated_Results()
    {
        // Arrange
        var cars = Enumerable.Range(1, 15).Select(i => new Car
        {
            Make = $"Make{i}",
            Model = $"Model{i}",
            Year = 2020,
            Color = "Black",
            Price = 20000 + i * 1000,
            Mileage = 50000 + i * 1000
        }).ToList();

        DbContext.Cars.AddRange(cars);
        await SaveAndClearTracking();

        // Act
        var response = await Client.GetAsync($"{BaseUrl}?pageIndex=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<PaginationResult<CarDto>>(response);
        result.Should().NotBeNull();
        result!.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(15);
        result.Data.Should().HaveCount(10);
    }

    #endregion

    #region PUT Tests

    [Fact]
    public async Task Update_Should_Update_Car_When_Valid()
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

        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Red",
            Price = 21000,
            Mileage = 65000,
            IsAvailable = false
        };

        // Act
        var response = await PutAsJsonAsync($"{BaseUrl}/{car.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<CarDto>(response);
        result.Should().NotBeNull();
        result!.Color.Should().Be("Red");
        result.Price.Should().Be(21000);
        result.Mileage.Should().Be(65000);
        result.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Update_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000
        };

        // Act
        var response = await PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Should_Return_BadRequest_When_Mileage_Decreases()
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

        var updateDto = new CarUpsertDto
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 50000
        };

        // Act
        var response = await PutAsJsonAsync($"{BaseUrl}/{car.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Errors.Should().Contain(e => e.Contains("Mileage cannot decrease"));
    }

    #endregion

    #region PATCH Tests

    [Fact]
    public async Task Patch_Should_Update_Only_Provided_Fields()
    {
        // Arrange
        var car = new Car
        {
            Make = "Renault",
            Model = "Clio",
            Year = 2020,
            Color = "Black",
            Price = 22000,
            Mileage = 62000,
            IsAvailable = true
        };
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        var patchDto = new CarPatchDto
        {
            Price = 21000,
            IsAvailable = false
        };

        // Act
        var response = await PatchAsJsonAsync($"{BaseUrl}/{car.Id}", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponse<CarDto>(response);
        result.Should().NotBeNull();
        result!.Price.Should().Be(21000);
        result.IsAvailable.Should().BeFalse();
        result.Make.Should().Be("Renault");
        result.Model.Should().Be("Clio");
        result.Mileage.Should().Be(62000);
    }

    [Fact]
    public async Task Patch_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var patchDto = new CarPatchDto
        {
            Price = 21000
        };

        // Act
        var response = await PatchAsJsonAsync($"{BaseUrl}/{nonExistentId}", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_Should_Return_BadRequest_When_Mileage_Decreases()
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

        var patchDto = new CarPatchDto
        {
            Mileage = 50000
        };

        // Act
        var response = await PatchAsJsonAsync($"{BaseUrl}/{car.Id}", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Errors.Should().Contain(e => e.Contains("Mileage cannot decrease"));
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task Delete_Should_Remove_Car_When_Exists()
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
        var response = await Client.DeleteAsync($"{BaseUrl}/{car.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"{BaseUrl}/{car.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
