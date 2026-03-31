# Error Handling Strategy - Result Pattern vs Exceptions

This template uses **FluentResults** for handling expected business failures and **Exceptions** for truly unexpected infrastructure/system failures.

## Table of Contents
- [Why This Approach?](#why-this-approach)
- [When to Use Result Pattern](#when-to-use-result-pattern)
- [When to Use Exceptions](#when-to-use-exceptions)
- [Implementation Guide](#implementation-guide)
- [Examples](#examples)

---

## Why This Approach?

### Problems with Using Exceptions for Control Flow
- **Hidden failure modes**: Method signatures don't show what can fail
- **Poor diagnostics**: Lost context as exceptions bubble up
- **Easy to miss**: Forgot try-catch? Runtime crash
- **Technical debt**: Harder to maintain and debug long-term

### Benefits of Result Pattern
- **Explicit error handling**: Failures are visible in return types
- **Better diagnostics**: Chain failure context through multiple layers
- **Compile-time safety**: Compiler ensures you handle results
- **More maintainable**: New developers see what can fail immediately
- **Better performance**: No exception stack unwinding overhead

---

## When to Use Result Pattern

Use `Result<T>` for **expected business failures** that are part of normal application flow:

### ✅ Business Rule Violations
```csharp
if (carDto.Year < 1900 || carDto.Year > DateTime.Now.Year + 1)
{
    return Result.Fail(new BusinessRuleError(
        "Year must be between 1900 and next year.",
        ErrorCode.INVALID_YEAR));
}
```

### ✅ Resource Not Found
```csharp
var car = await _carRepository.GetByIdAsync(id);
if (car == null)
{
    return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
}
```

### ✅ Validation Errors
```csharp
if (string.IsNullOrWhiteSpace(carDto.Make))
{
    return Result.Fail(new ValidationFailureError(
        "Make is required",
        ErrorCode.VALIDATION_ERROR));
}
```

### ✅ Business Logic Failures
- Insufficient permissions (user doesn't own the resource)
- State conflicts (trying to delete an active subscription)
- Duplicate entries (email already exists)
- Missing prerequisites (can't checkout without items in cart)

---

## When to Use Exceptions

Use **Exceptions** for **truly unexpected failures** that indicate system/infrastructure problems:

### ❌ Infrastructure Failures
- Database connection lost
- File system full / permission denied
- External API unavailable
- Network timeouts

### ❌ Programming Errors
- NullReferenceException
- IndexOutOfRangeException
- InvalidOperationException (when it indicates a bug)

### ❌ System-Level Failures
- OutOfMemoryException
- StackOverflowException
- Configuration missing/invalid

### Example from GenerateCarReportAsync
```csharp
try
{
    await File.WriteAllTextAsync(filePath, reportContent);
    return Result.Ok(filePath);
}
catch (UnauthorizedAccessException ex)
{
    // Unexpected infrastructure failure - could let it bubble to middleware
    // Or convert to Result if you want to handle it gracefully
    return Result.Fail(new DatabaseError(
        "Access denied when writing report to disk",
        ErrorCode.DATABASE_ERROR))
        .WithError(ex.Message);
}
// Other exceptions (OutOfMemoryException, etc.) bubble up to ExceptionHandlingMiddleware
```

---

## Implementation Guide

### 1. Service Layer

Return `Result<T>` from service methods:

```csharp
public async Task<Result<CarDto>> GetByIdAsync(Guid id)
{
    var car = await _carRepository.GetByIdAsync(id);

    if (car == null)
    {
        return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
    }

    return Result.Ok(_mapper.Map<CarDto>(car));
}
```

### 2. Controller Layer

All controllers inherit from `BaseApiController` which provides the `HandleFailure` method:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CarsController : BaseApiController
{
    public async Task<ActionResult<CarDto>> GetById(Guid id)
    {
        var result = await _carService.GetByIdAsync(id);

        if (result.IsFailed)
        {
            return HandleFailure(result); // Inherited from BaseApiController
        }

        return Ok(result.Value);
    }
}
```

The `HandleFailure` method in `BaseApiController`:

```csharp
protected ActionResult HandleFailure(ResultBase result)
{
    var firstError = result.Errors.FirstOrDefault();

    if (firstError is ApplicationError appError)
    {
        var errorResponse = ApiResponse<object>.ErrorResponse(
            message: appError.Message,
            errorCode: appError.ErrorCode,
            errors: result.Errors.Select(e => e.Message).ToList(),
            traceId: HttpContext.TraceIdentifier
        );

        return appError switch
        {
            NotFoundError => NotFound(errorResponse),
            ValidationFailureError => BadRequest(errorResponse),
            BusinessRuleError => BadRequest(errorResponse),
            DatabaseError => StatusCode(500, errorResponse),
            _ => StatusCode(500, errorResponse)
        };
    }

    // Fallback...
}
```

### 3. Base Controller

Located in `DotnetApiTemplate/Controllers/BaseApiController.cs`:

All API controllers should inherit from `BaseApiController` to get access to:
- **HandleFailure(ResultBase result)** - Converts Result failures to HTTP responses
- Future common functionality (auth helpers, logging, etc.)

**Important:** Each controller must add these attributes:
- `[ApiController]` - Enables automatic model validation and binding
- `[ApiVersion("1.0")]` - Specifies the API version
- `[Route("api/v{version:apiVersion}/[controller]")]` - Defines the route template

**Example:**
```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : BaseApiController
{
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var result = await _productService.GetByIdAsync(id);
        if (result.IsFailed)
            return HandleFailure(result); // From base class

        return Ok(result.Value);
    }
}
```

### 4. Custom Error Classes

Located in `DotnetApiTemplate.Application/Common/ApplicationError.cs`:

- **ApplicationError** - Base class with ErrorCode integration
- **NotFoundError** - Resource not found (404)
- **ValidationFailureError** - Input validation failures (400)
- **BusinessRuleError** - Business rule violations (400)
- **DatabaseError** - Database/persistence errors (500)

### 5. Exception Middleware

The `ExceptionHandlingMiddleware` now only handles **unexpected exceptions**:

- FluentValidation exceptions (from [ApiController])
- Database connection failures (DbUpdateException)
- File system errors (IOException, UnauthorizedAccessException)
- Network errors (HttpRequestException, TimeoutException)
- All other unexpected runtime errors

**Note:** `TaskCanceledException` and `OperationCanceledException` are **NOT** logged as errors. These occur when clients disconnect or cancel requests, which is normal behavior and doesn't indicate a problem with your application.

---

## Examples

### Example 1: Simple CRUD with Result Pattern

**Service:**
```csharp
public async Task<Result> DeleteAsync(Guid id)
{
    var car = await _carRepository.GetByIdAsync(id);

    if (car == null)
    {
        return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
    }

    await _carRepository.DeleteAsync(id);
    return Result.Ok();
}
```

**Controller:**
```csharp
[HttpDelete("{id:guid}")]
public async Task<ActionResult> Delete(Guid id)
{
    var result = await _carService.DeleteAsync(id);

    if (result.IsFailed)
    {
        return HandleFailure(result);
    }

    return NoContent();
}
```

### Example 2: Chaining Results with Context

```csharp
public async Task<Result<OrderDto>> ProcessOrderAsync(Guid orderId)
{
    var orderResult = await GetOrderAsync(orderId);
    if (orderResult.IsFailed)
    {
        return orderResult.ToResult(); // Propagate failure with context
    }

    var order = orderResult.Value;

    var paymentResult = await ProcessPaymentAsync(order);
    if (paymentResult.IsFailed)
    {
        return paymentResult.ToResult()
            .WithError($"Failed to process payment for order {orderId}"); // Add context
    }

    return Result.Ok(order);
}
```

### Example 3: Mixing Results and Exceptions

From `GenerateCarReportAsync`:

```csharp
public async Task<Result<string>> GenerateCarReportAsync(Guid id)
{
    // Use Result for expected failures (car not found)
    var car = await _carRepository.GetByIdAsync(id);
    if (car == null)
    {
        return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
    }

    try
    {
        // Operations that can throw unexpected exceptions
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        Directory.CreateDirectory(filePath); // Can throw IOException

        await File.WriteAllTextAsync(filePath, content); // Can throw UnauthorizedAccessException

        return Result.Ok(filePath);
    }
    catch (IOException ex)
    {
        // Option 1: Convert to Result for graceful handling
        return Result.Fail(new DatabaseError("Failed to write report", ErrorCode.DATABASE_ERROR));

        // Option 2: Let it bubble to ExceptionHandlingMiddleware
        // throw;
    }
}
```

---

## Testing

### Testing Result Pattern

```csharp
[Fact]
public async Task GetByIdAsync_WhenCarNotFound_ReturnsFailedResult()
{
    // Arrange
    _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync((Car)null);

    // Act
    var result = await _carService.GetByIdAsync(Guid.NewGuid());

    // Assert
    Assert.True(result.IsFailed);
    Assert.IsType<NotFoundError>(result.Errors.First());
}

[Fact]
public async Task GetByIdAsync_WhenCarExists_ReturnsSuccessResult()
{
    // Arrange
    var car = new Car { Id = Guid.NewGuid(), Make = "Toyota" };
    _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync(car);

    // Act
    var result = await _carService.GetByIdAsync(car.Id);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("Toyota", result.Value.Make);
}
```

---

## Migration Guide

### Converting Exception-Based Code to Result Pattern

**Before (Exception-based):**
```csharp
public async Task<CarDto> GetByIdAsync(Guid id)
{
    var car = await _repository.GetByIdAsync(id);
    if (car == null)
    {
        throw new NotFoundException($"Car {id} not found");
    }
    return _mapper.Map<CarDto>(car);
}
```

**After (Result pattern):**
```csharp
public async Task<Result<CarDto>> GetByIdAsync(Guid id)
{
    var car = await _repository.GetByIdAsync(id);
    if (car == null)
    {
        return Result.Fail(new NotFoundError("Resource not found", ErrorCode.CAR_NOT_FOUND));
    }
    return Result.Ok(_mapper.Map<CarDto>(car));
}
```

---

## Best Practices

1. **Use BaseApiController**: Always inherit from `BaseApiController` for all API controllers to get access to `HandleFailure` and other common functionality
2. **Be Consistent**: Use Result pattern for all expected business failures
3. **Add Context**: Use `.WithError()` to add diagnostic information as failures propagate
4. **Don't Mix**: Avoid using both exceptions and Results for the same type of failure
5. **Document Intent**: Use XML comments to document what Results can be returned
6. **Keep It Simple**: Don't over-engineer - simple failures don't need custom error classes
7. **Log Appropriately**: Log Results at service layer, Exceptions at middleware

---

---

## Validation et Gestion d'Erreurs

### ValidationFilter et FluentValidation

Ce projet utilise un **ValidationFilter custom** pour intégrer FluentValidation avec les ErrorCodes.

**Pourquoi un filtre custom ?**
- L'attribut `[ApiController]` ne donne pas accès aux ErrorCodes de FluentValidation
- Permet de retourner des erreurs au format `ApiResponse` avec ErrorCode pour l'i18n frontend
- Exécute manuellement les validateurs pour accéder aux métadonnées complètes

**Flux de validation :**

```
1. Requête HTTP arrive
   ↓
2. Model Binding (désérialisation JSON → DTO)
   ↓
3. ValidationFilter s'exécute
   ↓
4. ValidationFilter cherche un validator FluentValidation pour le DTO
   ↓
5. ValidationFilter exécute le validator manuellement
   ↓
6. Si validation échoue → BadRequest avec ApiResponse (ErrorCode inclus)
   ↓
7. Si validation passe → Controller s'exécute
```

**Configuration requise :**

`Program.cs` / `ApplicationServicesExtensions.cs` :
```csharp
// Enregistrer les validateurs FluentValidation
services.AddValidatorsFromAssemblyContaining<CarUpsertDtoValidator>();

// Désactiver la validation automatique (on la gère manuellement)
services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
```

**Exemple de validateur avec ErrorCode :**

```csharp
public class CarUpsertDtoValidator : AbstractValidator<CarUpsertDto>
{
    public CarUpsertDtoValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty()
            .WithMessage("Make is required.")
            .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING)); // ✅ ErrorCode pour i18n

        RuleFor(x => x.Year)
            .GreaterThan(1900)
            .WithMessage("Year must be after 1900.")
            .WithErrorCode(nameof(ErrorCode.INVALID_YEAR));
    }
}
```

**Réponse d'erreur de validation :**

```json
{
  "success": false,
  "data": null,
  "message": "One or more validation errors occurred",
  "errorCode": "VALIDATION_ERROR",
  "errors": [
    "Make: Make is required.",
    "Year: Year must be after 1900."
  ],
  "validationErrors": [
    {
      "field": "Make",
      "message": "Make is required.",
      "errorCode": "REQUIRED_FIELD_MISSING",
      "attemptedValue": ""
    },
    {
      "field": "Year",
      "message": "Year must be after 1900.",
      "errorCode": "INVALID_YEAR",
      "attemptedValue": 1800
    }
  ],
  "traceId": "0HN7..."
}
```

Le frontend peut utiliser `errorCode` pour afficher des messages traduits.

---

## ResponseWrapperMiddleware

Toutes les réponses API (succès et erreurs) sont enveloppées dans un format standardisé `ApiResponse<T>`.

**Format de réponse standardisé :**

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }           // true/false
    public T? Data { get; set; }                // Données de la réponse
    public string? Message { get; set; }        // Message optionnel
    public string? ErrorCode { get; set; }      // Code d'erreur (enum en string)
    public List<string>? Errors { get; set; }   // Liste d'erreurs textuelles
    public List<ValidationError>? ValidationErrors { get; set; }  // Erreurs de validation détaillées
    public string? TraceId { get; set; }        // ID de trace pour debugging
}
```

**Réponse de succès :**

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "make": "Renault",
    "model": "Clio",
    "year": 2020,
    "price": 22000
  },
  "message": null,
  "errorCode": null,
  "errors": null,
  "validationErrors": null,
  "traceId": "0HN7..."
}
```

**Le middleware skip certains endpoints :**
- `/health` - Health checks (ne wrap pas)
- `/swagger` - Documentation Swagger (ne wrap pas)
- `/openapi` - Spec OpenAPI (ne wrap pas)
- Status codes 204 (No Content) et 304 (Not Modified) - Pas de body autorisé

---

## Resources

- [FluentResults Documentation](https://github.com/altmann/FluentResults)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Error Handling Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Filters](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters)
