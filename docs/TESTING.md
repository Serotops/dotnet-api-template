# Guide des Tests - DotnetApiTemplate

## 📚 Table des Matières

- [Vue d'ensemble](#vue-densemble)
- [Tests Unitaires](#-tests-unitaires-unit-tests)
- [Tests d'Intégration](#-tests-dintégration-integration-tests)
- [Comparaison](#-comparaison-détaillée)
- [Quand utiliser quoi ?](#-quand-utiliser-quoi-)
- [Exécution des tests](#-exécution-des-tests)
- [Structure du projet](#-structure-du-projet)

---

## Vue d'ensemble

Ce projet contient **115 tests** répartis en deux catégories :

- ✅ **79 Tests Unitaires** (~300ms) - Tests rapides et isolés
- ✅ **36 Tests d'Intégration** (~2-11s) - Tests complets de bout en bout

```
✅ Unit Tests:           79 tests passed in 309ms
✅ Integration Tests:    36 tests passed in 2s
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ TOTAL:               115 tests passed ✨
```

---

## 🎯 **Tests Unitaires (Unit Tests)**

### Objectif

Tester **une seule unité de code de manière isolée** (une méthode, une classe) sans dépendances externes.

### Caractéristiques

| Aspect | Description |
|--------|-------------|
| **Vitesse** | ⚡ Très rapide (300ms pour 79 tests) |
| **Isolation** | ✅ Totale - Utilisent des **mocks** (fausses dépendances) |
| **Base de données** | ❌ Aucune - Tout est simulé |
| **Réseau/HTTP** | ❌ Pas d'appels réels |
| **Déterminisme** | ✅ Toujours le même résultat |
| **Feedback** | ✅ Immédiat - Identifie exactement quelle méthode échoue |

### Exemples dans le Projet

#### 1. Tests de Validation (CarUpsertDtoValidatorTests.cs)

```csharp
[Fact]
public void Should_Have_Error_When_Make_Is_Empty()
{
    // Arrange
    var dto = new CarUpsertDto { Make = "" };

    // Act
    var result = _validator.TestValidate(dto);

    // Assert
    result.ShouldHaveValidationErrorFor(x => x.Make)
        .WithErrorCode(nameof(ErrorCode.REQUIRED_FIELD_MISSING));
}
```

**Ce qu'on teste :**
- ✅ La règle de validation FluentValidation
- ✅ Que l'ErrorCode correct est retourné
- ❌ Pas de base de données
- ❌ Pas de HTTP

---

#### 2. Tests de Service avec Mocks (CarServiceTests.cs)

```csharp
[Fact]
public async Task UpdateAsync_Should_Fail_When_Mileage_Decreases()
{
    // Arrange - MOCK du repository
    var mockRepository = new Mock<ICarRepository>();
    var existingCar = new Car { Id = carId, Mileage = 62000 };

    mockRepository.Setup(x => x.GetByIdAsync(carId))
        .ReturnsAsync(existingCar);  // ⚠️ Fausse réponse

    var service = new CarService(mockRepository.Object, mockMapper.Object);
    var updateDto = new CarUpsertDto { Mileage = 50000 }; // Diminution invalide

    // Act
    var result = await service.UpdateAsync(carId, updateDto);

    // Assert
    result.IsFailed.Should().BeTrue();
    result.Errors[0].Should().BeOfType<BusinessRuleError>();

    // Vérifie que UpdateAsync n'a JAMAIS été appelé
    mockRepository.Verify(x => x.UpdateAsync(It.IsAny<Car>()), Times.Never);
}
```

**Ce qu'on teste :**
- ✅ La **logique métier** du service (règle : le kilométrage ne peut pas diminuer)
- ✅ Que le service appelle (ou n'appelle pas) les bonnes méthodes du repository
- ✅ Le type d'erreur retourné (BusinessRuleError)
- ❌ Pas de vraie base de données
- ❌ Pas de vraies requêtes SQL

---

### Avantages des Tests Unitaires

✅ **Rapides** - Exécution immédiate, idéal pour TDD
✅ **Isolés** - Un échec identifie exactement le problème
✅ **Simples** - Pas de setup complexe de base de données
✅ **Fiables** - Pas d'effets de bord entre tests
✅ **CI/CD** - Parfait pour les pipelines (très rapides)

---

## 🔗 **Tests d'Intégration (Integration Tests)**

### Objectif

Tester **l'intégration complète de plusieurs composants** avec de vraies dépendances.

### Caractéristiques

| Aspect | Description |
|--------|-------------|
| **Vitesse** | 🐢 Plus lent (2-11 secondes pour 36 tests) |
| **Portée** | 🔗 Plusieurs composants ensemble |
| **Base de données** | ✅ InMemory Database (équivalent à une vraie DB) |
| **HTTP** | ✅ Vrais appels HTTP via `HttpClient` |
| **Stack complète** | ✅ Middleware → Controller → Service → Repository → DB |
| **Isolation** | ⚠️ Partielle (DB partagée, nettoyée entre tests) |

### Exemples dans le Projet

#### 1. Tests de Controller HTTP (CarsControllerTests.cs)

```csharp
[Fact]
public async Task Update_Should_Return_BadRequest_When_Mileage_Decreases()
{
    // Arrange - VRAIE base de données InMemory
    var car = new Car
    {
        Make = "Renault",
        Model = "Clio",
        Year = 2020,
        Color = "Black",
        Price = 22000,
        Mileage = 62000
    };
    DbContext.Cars.Add(car);  // ✅ VRAIE insertion en DB
    await SaveAndClearTracking();

    var updateDto = new CarUpsertDto
    {
        Make = "Renault",
        Model = "Clio",
        Year = 2020,
        Color = "Black",
        Price = 22000,
        Mileage = 50000  // Diminution - invalide
    };

    // Act - VRAI appel HTTP au controller
    var response = await PutAsJsonAsync($"/api/v1/cars/{car.Id}", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
    apiResponse.Success.Should().BeFalse();
    apiResponse.Errors.Should().Contain(e => e.Contains("Mileage cannot decrease"));
}
```

**Ce qu'on teste (TOUT le flow) :**
- ✅ Le **endpoint HTTP complet** (PUT /api/v1/cars/{id})
- ✅ Le **routing** ASP.NET Core
- ✅ La **désérialisation** JSON → CarUpsertDto
- ✅ Le **ValidationFilter** (middleware custom)
- ✅ Le **Controller** (CarsController.Update)
- ✅ Le **Service** (CarService.UpdateAsync avec logique métier)
- ✅ Le **Repository** (vraie requête vers InMemory DB)
- ✅ Le **ResponseWrapperMiddleware** (enveloppe ApiResponse)
- ✅ La **sérialisation** ApiResponse → JSON
- ✅ Le **code HTTP 400** retourné
- ✅ Le **format de réponse** final

---

#### 2. Tests de Repository (CarRepositoryTests.cs)

```csharp
[Fact]
public async Task GetFilteredAsync_Should_Filter_By_Make()
{
    // Arrange - Insertion en vraie DB InMemory
    var cars = new List<Car>
    {
        new Car { Make = "Renault", Model = "Clio", Year = 2020, Color = "Black", Price = 22000, Mileage = 62000 },
        new Car { Make = "Toyota", Model = "Corolla", Year = 2021, Color = "White", Price = 25000, Mileage = 30000 },
        new Car { Make = "Renault", Model = "Megane", Year = 2021, Color = "Blue", Price = 28000, Mileage = 40000 }
    };
    DbContext.Cars.AddRange(cars);
    await SaveAndClearTracking();

    var filterParams = new CarParams
    {
        Make = "Renault",
        PageIndex = 1,
        PageSize = 10
    };

    // Act - Vraie requête LINQ vers InMemory DB
    var result = await _repository.GetFilteredAsync(filterParams);

    // Assert
    result.TotalItems.Should().Be(2);
    result.Data.Should().HaveCount(2);
    result.Data.Should().OnlyContain(c => c.Make == "Renault");
}
```

**Ce qu'on teste :**
- ✅ Les **requêtes LINQ** complexes
- ✅ Le **filtrage** par Make
- ✅ La **pagination** (PageIndex, PageSize, TotalItems)
- ✅ La **vraie base de données** InMemory
- ✅ Que les données sont correctement persistées et récupérées

---

### Avantages des Tests d'Intégration

✅ **Réalistes** - Testent le vrai comportement de l'API
✅ **Complets** - Détectent les problèmes d'intégration entre couches
✅ **Confiance** - Garantissent que tout fonctionne ensemble
✅ **Endpoints** - Valident les contrats HTTP (routes, status codes, JSON)
✅ **Middlewares** - Testent les filtres, validation, response wrapping

---

## 📊 **Comparaison Détaillée**

| Critère | Unit Tests | Integration Tests |
|---------|-----------|-------------------|
| **Vitesse** | ⚡ Très rapide (300ms pour 79 tests) | 🐢 Plus lent (11s pour 36 tests) |
| **Portée** | 🎯 Une seule classe/méthode | 🔗 Plusieurs composants ensemble |
| **Dépendances** | 🎭 Mockées (fausses) | ✅ Réelles (InMemory DB, HttpClient) |
| **Base de données** | ❌ Aucune | ✅ InMemory Database |
| **HTTP/Réseau** | ❌ Aucun | ✅ WebApplicationFactory + HttpClient |
| **Isolation** | ✅ Totale | ⚠️ Partielle (partage la DB entre tests) |
| **Maintenance** | ✅ Facile (moins de setup) | ⚠️ Plus complexe (plus de setup) |
| **Feedback** | ✅ Immédiat (quelle méthode échoue) | ⚠️ Large (tout le flow peut échouer) |
| **CI/CD** | ✅ Parfait (très rapide) | ⚠️ Acceptable (plus lent) |
| **Debugging** | ✅ Facile (scope limité) | ⚠️ Plus difficile (scope large) |

---

## 🎓 **Quand Utiliser Quoi ?**

### Utilisez les **Unit Tests** pour :

- ✅ Tester la **logique métier complexe**
- ✅ Tester les **règles de validation** (FluentValidation)
- ✅ Tester les **calculs/algorithmes**
- ✅ Avoir un **feedback rapide** pendant le développement (TDD)
- ✅ Exécuter dans la **CI/CD** (très rapide)
- ✅ Tester des **cas limites** (edge cases)
- ✅ Vérifier le **comportement d'une méthode** spécifique

### Utilisez les **Integration Tests** pour :

- ✅ Tester les **endpoints API complets** (GET/POST/PUT/PATCH/DELETE)
- ✅ Vérifier que **tous les composants fonctionnent ensemble**
- ✅ Tester les **requêtes de base de données complexes** (filtres, pagination)
- ✅ Valider le **format de réponse final** (ApiResponse)
- ✅ Tester les **middlewares et filtres** (ValidationFilter, ResponseWrapper)
- ✅ Détecter les **problèmes d'intégration** entre couches
- ✅ Tester le **routing** et la **sérialisation/désérialisation** JSON

---

## 🔍 **Exemple Concret : Scénario "Le kilométrage ne peut pas diminuer"**

### Unit Test (CarServiceTests.cs)

```csharp
// ✅ Teste UNIQUEMENT la logique du service
// - Mock du repository
// - Pas de DB, pas de HTTP
// - Vérifie que le service retourne IsFailed
// - Vérifie que UpdateAsync n'est jamais appelé

[Fact]
public async Task UpdateAsync_Should_Fail_When_Mileage_Decreases()
{
    var mockRepository = new Mock<ICarRepository>();
    mockRepository.Setup(x => x.GetByIdAsync(carId))
        .ReturnsAsync(new Car { Mileage = 62000 });

    var service = new CarService(mockRepository.Object, ...);
    var result = await service.UpdateAsync(carId, new CarUpsertDto { Mileage = 50000 });

    result.IsFailed.Should().BeTrue();
    mockRepository.Verify(x => x.UpdateAsync(...), Times.Never);
}
```

---

### Integration Test (CarsControllerTests.cs)

```csharp
// ✅ Teste TOUT le flow de bout en bout
// - Vraie requête HTTP PUT
// - Passe par ValidationFilter (validation FluentValidation)
// - Passe par CarsController.Update
// - Passe par CarService.UpdateAsync (avec sa vraie logique)
// - Passe par CarRepository.UpdateAsync (vraie requête DB)
// - Passe par ResponseWrapperMiddleware
// - Retourne un HTTP 400 BadRequest avec ApiResponse

[Fact]
public async Task Update_Should_Return_BadRequest_When_Mileage_Decreases()
{
    var car = new Car { Mileage = 62000 };
    DbContext.Cars.Add(car);
    await SaveAndClearTracking();

    var response = await PutAsJsonAsync($"/api/v1/cars/{car.Id}",
        new CarUpsertDto { Mileage = 50000 });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var apiResponse = await DeserializeResponse<ApiResponse<object>>(response);
    apiResponse.Errors.Should().Contain(e => e.Contains("Mileage cannot decrease"));
}
```

---

## 🚀 **Exécution des Tests**

### Tous les tests

```bash
dotnet test
```

### Tests unitaires uniquement

```bash
dotnet test tests/DotnetApiTemplate.UnitTests/DotnetApiTemplate.UnitTests.csproj
```

### Tests d'intégration uniquement

```bash
dotnet test tests/DotnetApiTemplate.IntegrationTests/DotnetApiTemplate.IntegrationTests.csproj
```

### Avec verbosité détaillée

```bash
dotnet test --logger "console;verbosity=detailed"
```

### En mode Release

```bash
dotnet test --configuration Release
```

---

## 📁 **Structure du Projet**

```
DotnetApiTemplate/
├── src/
│   ├── DotnetApiTemplate.API/
│   ├── DotnetApiTemplate.Application/
│   ├── DotnetApiTemplate.Domain/
│   ├── DotnetApiTemplate.Infrastructure/
│   └── DotnetApiTemplate.Persistence/
│
├── tests/
│   ├── DotnetApiTemplate.UnitTests/                    (79 tests)
│   │   ├── Application/
│   │   │   ├── Services/
│   │   │   │   └── CarServiceTests.cs           → Logique métier avec mocks
│   │   │   └── Validators/
│   │   │       ├── CarUpsertDtoValidatorTests.cs → Règles de validation
│   │   │       └── CarPatchDtoValidatorTests.cs  → Règles de validation
│   │   └── Helpers/
│   │       └── TestDataBuilder.cs               → Builders pour créer des données de test
│   │
│   └── DotnetApiTemplate.IntegrationTests/             (36 tests)
│       ├── Base/
│       │   └── IntegrationTestBase.cs           → Classe de base pour les tests
│       ├── Fixtures/
│       │   └── ApiTestFactory.cs                → WebApplicationFactory custom
│       ├── Controllers/
│       │   └── CarsControllerTests.cs           → Tests HTTP complets
│       └── Repositories/
│           └── CarRepositoryTests.cs            → Tests de requêtes DB
```

---

## 🛠️ **Technologies Utilisées**

### Tests Unitaires

- **xUnit** - Framework de tests
- **FluentAssertions** - Assertions lisibles et expressives
- **Moq** - Framework de mocking
- **FluentValidation.TestHelper** - Tests de validateurs

### Tests d'Intégration

- **xUnit** - Framework de tests
- **FluentAssertions** - Assertions lisibles
- **Microsoft.AspNetCore.Mvc.Testing** - WebApplicationFactory
- **Microsoft.EntityFrameworkCore.InMemory** - Base de données en mémoire

---

## 📈 **Couverture de Test**

### Tests Unitaires (79 tests)

| Composant | Tests | Couverture |
|-----------|-------|------------|
| CarUpsertDtoValidator | 27 tests | 100% |
| CarPatchDtoValidator | 24 tests | 100% |
| CarService | 28 tests | 100% |

### Tests d'Intégration (36 tests)

| Composant | Tests | Couverture |
|-----------|-------|------------|
| CarsController (HTTP) | 19 tests | Tous les endpoints |
| CarRepository (DB) | 17 tests | CRUD + Filtrage complet |

---

## 🎯 **Bonnes Pratiques**

### Unit Tests

✅ **AAA Pattern** (Arrange, Act, Assert)
✅ **Noms descriptifs** : `Should_ReturnError_When_MileageDecreases`
✅ **Un seul Assert par test** (sauf cas exceptionnels)
✅ **Mocks isolés** : Chaque test crée ses propres mocks
✅ **Pas d'effets de bord** : Tests indépendants

### Integration Tests

✅ **Nettoyage de la DB** : `ClearDatabase()` avant chaque test
✅ **DB unique par test class** : Évite les conflits
✅ **Tests complets** : Valider toute la stack
✅ **Assertions multiples** : Vérifier status code + body + headers
✅ **Données réalistes** : Utiliser des données représentatives

---

## 📝 **Résumé**

| Aspect | Unit Tests | Integration Tests |
|--------|-----------|-------------------|
| **But** | Tester la logique isolée | Tester l'intégration complète |
| **Rapidité** | ⚡ Très rapide | 🐢 Plus lent |
| **Isolation** | ✅ Totale (mocks) | ⚠️ Partielle (vraie DB) |
| **Complexité** | ✅ Simple | ⚠️ Plus complexe |
| **Valeur** | Feedback immédiat | Confiance globale |
| **Quand ?** | Développement continu | Validation finale |

**Les deux types sont complémentaires et essentiels pour une couverture de test complète !** 🎉

---

## 📚 **Ressources**

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq](https://github.com/moq/moq4)
- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [FluentValidation Testing](https://docs.fluentvalidation.net/en/latest/testing.html)

---

**Généré pour DotnetApiTemplate - Clean Architecture .NET API** 🚀
