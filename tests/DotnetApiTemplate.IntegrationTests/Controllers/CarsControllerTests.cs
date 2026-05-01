using DotnetApiTemplate.Application.Common;
using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Common;
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

        var apiResponse = await DeserializeResponse<ApiResponse<CarDto>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Make.Should().Be("Renault");
        apiResponse.Data.Model.Should().Be("Clio");
        apiResponse.Data.Year.Should().Be(2020);
        apiResponse.Data.Id.Should().NotBeEmpty();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(apiResponse.Data.Id.ToString());
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Make_Is_Empty()
    {
        // Arrange
        var dto = new CarUpsertDto
        {
            Make = "", // Invalid
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
            Make = "", // Invalid
            Model = "", // Invalid
            Year = 1800, // Invalid (before 1900)
            Color = "",  // Invalid
            Price = -100, // Invalid (negative)
            Mileage = -50 // Invalid (negative)
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

        var apiResponse = await DeserializeResponse<ApiResponse<CarDto>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Id.Should().Be(car.Id);
        apiResponse.Data.Make.Should().Be("Toyota");
        apiResponse.Data.Model.Should().Be("Corolla");
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

        var apiResponse = await DeserializeResponse<ApiResponse<List<CarDto>>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data.Should().HaveCount(2);
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

        var apiResponse = await DeserializeResponse<ApiResponse<PaginationResult<CarDto>>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.PageIndex.Should().Be(1);
        apiResponse.Data.PageSize.Should().Be(10);
        apiResponse.Data.TotalItems.Should().Be(15);
        apiResponse.Data.Data.Should().HaveCount(10);
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
            Color = "Red", // Changed
            Price = 21000, // Changed
            Mileage = 65000, // Increased - valid
            IsAvailable = false // Changed
        };

        // Act
        var response = await PutAsJsonAsync($"{BaseUrl}/{car.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<CarDto>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Color.Should().Be("Red");
        apiResponse.Data.Price.Should().Be(21000);
        apiResponse.Data.Mileage.Should().Be(65000);
        apiResponse.Data.IsAvailable.Should().BeFalse();
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
            Mileage = 50000 // Decreased - invalid
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
            // Other fields are null - should not be updated
        };

        // Act
        var response = await PatchAsJsonAsync($"{BaseUrl}/{car.Id}", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<CarDto>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Price.Should().Be(21000);
        apiResponse.Data.IsAvailable.Should().BeFalse();
        apiResponse.Data.Make.Should().Be("Renault"); // Should not change
        apiResponse.Data.Model.Should().Be("Clio"); // Should not change
        apiResponse.Data.Mileage.Should().Be(62000); // Should not change
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
            Mileage = 50000 // Decreased - invalid
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

        // Verify car is deleted
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

    #region GenerateReport Tests

    [Fact]
    public async Task GenerateReport_Should_Create_Report_When_Car_Exists()
    {
        // Arrange
        var car = new Car
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
        DbContext.Cars.Add(car);
        await SaveAndClearTracking();

        // Act
        var response = await Client.PostAsync($"{BaseUrl}/{car.Id}/report", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await DeserializeResponse<ApiResponse<string>>(response);
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNullOrEmpty();
        apiResponse.Data.Should().Contain("car-report-");
        apiResponse.Data.Should().EndWith(".txt");

        // Clean up - delete the generated report file if it exists
        if (File.Exists(apiResponse.Data))
        {
            File.Delete(apiResponse.Data);
        }
    }

    [Fact]
    public async Task GenerateReport_Should_Return_NotFound_When_Car_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"{BaseUrl}/{nonExistentId}/report", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
