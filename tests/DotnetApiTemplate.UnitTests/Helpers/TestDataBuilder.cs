using DotnetApiTemplate.Application.DTOs;
using DotnetApiTemplate.Domain.Entities;

namespace DotnetApiTemplate.UnitTests.Helpers;

/// <summary>
/// Test data builder providing fluent API for creating test objects with sensible defaults.
/// Makes tests more readable and maintainable by centralizing test data creation.
/// </summary>
public static class TestDataBuilder
{
    #region Car Entity Builders

    public static CarBuilder BuildCar() => new CarBuilder();

    public class CarBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _make = "Renault";
        private string _model = "Clio";
        private int _year = 2020;
        private string _color = "Black";
        private decimal _price = 22000;
        private string? _vin = "12345678901234567";
        private int _mileage = 62000;
        private bool _isAvailable = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime? _modifiedAt = null;

        public CarBuilder WithId(Guid id)
        {
            _id = id;
            return this;
        }

        public CarBuilder WithMake(string make)
        {
            _make = make;
            return this;
        }

        public CarBuilder WithModel(string model)
        {
            _model = model;
            return this;
        }

        public CarBuilder WithYear(int year)
        {
            _year = year;
            return this;
        }

        public CarBuilder WithColor(string color)
        {
            _color = color;
            return this;
        }

        public CarBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public CarBuilder WithVIN(string? vin)
        {
            _vin = vin;
            return this;
        }

        public CarBuilder WithMileage(int mileage)
        {
            _mileage = mileage;
            return this;
        }

        public CarBuilder WithIsAvailable(bool isAvailable)
        {
            _isAvailable = isAvailable;
            return this;
        }

        public CarBuilder WithCreatedAt(DateTime createdAt)
        {
            _createdAt = createdAt;
            return this;
        }

        public CarBuilder WithModifiedAt(DateTime? modifiedAt)
        {
            _modifiedAt = modifiedAt;
            return this;
        }

        public Car Build()
        {
            return new Car
            {
                Id = _id,
                Make = _make,
                Model = _model,
                Year = _year,
                Color = _color,
                Price = _price,
                VIN = _vin,
                Mileage = _mileage,
                IsAvailable = _isAvailable,
                CreatedAt = _createdAt,
                ModifiedAt = _modifiedAt
            };
        }
    }

    #endregion

    #region CarDto Builders

    public static CarDtoBuilder BuildCarDto() => new CarDtoBuilder();

    public class CarDtoBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _make = "Renault";
        private string _model = "Clio";
        private int _year = 2020;
        private string _color = "Black";
        private decimal _price = 22000;
        private string? _vin = "12345678901234567";
        private int _mileage = 62000;
        private bool _isAvailable = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime? _modifiedAt = null;

        public CarDtoBuilder WithId(Guid id)
        {
            _id = id;
            return this;
        }

        public CarDtoBuilder WithMake(string make)
        {
            _make = make;
            return this;
        }

        public CarDtoBuilder WithModel(string model)
        {
            _model = model;
            return this;
        }

        public CarDtoBuilder WithYear(int year)
        {
            _year = year;
            return this;
        }

        public CarDtoBuilder WithColor(string color)
        {
            _color = color;
            return this;
        }

        public CarDtoBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public CarDtoBuilder WithVIN(string? vin)
        {
            _vin = vin;
            return this;
        }

        public CarDtoBuilder WithMileage(int mileage)
        {
            _mileage = mileage;
            return this;
        }

        public CarDtoBuilder WithIsAvailable(bool isAvailable)
        {
            _isAvailable = isAvailable;
            return this;
        }

        public CarDtoBuilder WithCreatedAt(DateTime createdAt)
        {
            _createdAt = createdAt;
            return this;
        }

        public CarDtoBuilder WithModifiedAt(DateTime? modifiedAt)
        {
            _modifiedAt = modifiedAt;
            return this;
        }

        public CarDto Build()
        {
            return new CarDto
            {
                Id = _id,
                Make = _make,
                Model = _model,
                Year = _year,
                Color = _color,
                Price = _price,
                VIN = _vin,
                Mileage = _mileage,
                IsAvailable = _isAvailable,
                CreatedAt = _createdAt,
                ModifiedAt = _modifiedAt
            };
        }
    }

    #endregion

    #region CarUpsertDto Builders

    public static CarUpsertDtoBuilder BuildCarUpsertDto() => new CarUpsertDtoBuilder();

    public class CarUpsertDtoBuilder
    {
        private string _make = "Renault";
        private string _model = "Clio";
        private int _year = 2020;
        private string _color = "Black";
        private decimal _price = 22000;
        private string? _vin = "12345678901234567";
        private int _mileage = 62000;
        private bool _isAvailable = true;

        public CarUpsertDtoBuilder WithMake(string make)
        {
            _make = make;
            return this;
        }

        public CarUpsertDtoBuilder WithModel(string model)
        {
            _model = model;
            return this;
        }

        public CarUpsertDtoBuilder WithYear(int year)
        {
            _year = year;
            return this;
        }

        public CarUpsertDtoBuilder WithColor(string color)
        {
            _color = color;
            return this;
        }

        public CarUpsertDtoBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public CarUpsertDtoBuilder WithVIN(string? vin)
        {
            _vin = vin;
            return this;
        }

        public CarUpsertDtoBuilder WithMileage(int mileage)
        {
            _mileage = mileage;
            return this;
        }

        public CarUpsertDtoBuilder WithIsAvailable(bool isAvailable)
        {
            _isAvailable = isAvailable;
            return this;
        }

        public CarUpsertDto Build()
        {
            return new CarUpsertDto
            {
                Make = _make,
                Model = _model,
                Year = _year,
                Color = _color,
                Price = _price,
                VIN = _vin,
                Mileage = _mileage,
                IsAvailable = _isAvailable
            };
        }
    }

    #endregion

    #region CarPatchDto Builders

    public static CarPatchDtoBuilder BuildCarPatchDto() => new CarPatchDtoBuilder();

    public class CarPatchDtoBuilder
    {
        private string? _make = null;
        private string? _model = null;
        private int? _year = null;
        private string? _color = null;
        private decimal? _price = null;
        private string? _vin = null;
        private int? _mileage = null;
        private bool? _isAvailable = null;

        public CarPatchDtoBuilder WithMake(string? make)
        {
            _make = make;
            return this;
        }

        public CarPatchDtoBuilder WithModel(string? model)
        {
            _model = model;
            return this;
        }

        public CarPatchDtoBuilder WithYear(int? year)
        {
            _year = year;
            return this;
        }

        public CarPatchDtoBuilder WithColor(string? color)
        {
            _color = color;
            return this;
        }

        public CarPatchDtoBuilder WithPrice(decimal? price)
        {
            _price = price;
            return this;
        }

        public CarPatchDtoBuilder WithVIN(string? vin)
        {
            _vin = vin;
            return this;
        }

        public CarPatchDtoBuilder WithMileage(int? mileage)
        {
            _mileage = mileage;
            return this;
        }

        public CarPatchDtoBuilder WithIsAvailable(bool? isAvailable)
        {
            _isAvailable = isAvailable;
            return this;
        }

        public CarPatchDto Build()
        {
            return new CarPatchDto
            {
                Make = _make,
                Model = _model,
                Year = _year,
                Color = _color,
                Price = _price,
                VIN = _vin,
                Mileage = _mileage,
                IsAvailable = _isAvailable
            };
        }
    }

    #endregion
}
