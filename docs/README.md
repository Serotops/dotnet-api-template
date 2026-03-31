# 📚 Documentation DotnetApiTemplate

Bienvenue dans la documentation complète de l'DotnetApiTemplate. Ce répertoire contient des guides détaillés sur l'architecture, les patterns et les bonnes pratiques utilisés dans ce projet.

---

## 📖 Guides Disponibles

### 🧪 [TESTING.md](TESTING.md) - Guide des Tests
**Guide complet sur les tests unitaires et d'intégration**

Ce document explique :
- ✅ Différences entre tests unitaires et tests d'intégration
- ✅ Quand utiliser chaque type de test
- ✅ Comment exécuter les tests
- ✅ Structure des projets de test (115 tests au total)
- ✅ Exemples de code détaillés
- ✅ Bonnes pratiques

**Statistiques :**
- 79 Tests Unitaires (300ms) - Validators, Services avec mocks
- 36 Tests d'Intégration (2s) - Controllers HTTP, Repositories DB

---

### ⚠️ [ERROR_HANDLING.md](ERROR_HANDLING.md) - Gestion des Erreurs
**Stratégie Result Pattern vs Exceptions**

Ce document couvre :
- ✅ Pourquoi utiliser le Result Pattern (FluentResults)
- ✅ Quand utiliser Result vs Exception
- ✅ Custom error classes (NotFoundError, BusinessRuleError, etc.)
- ✅ BaseApiController et HandleFailure
- ✅ ExceptionHandlingMiddleware
- ✅ Exemples pratiques et migration guide

**Points clés :**
- **Result Pattern** → Échecs métier attendus (validation, not found, business rules)
- **Exceptions** → Erreurs système inattendues (DB connection, file system, etc.)

---

### 📡 [API_RESPONSE_FORMAT.md](API_RESPONSE_FORMAT.md) - Format des Réponses API
**Guide complet pour intégration frontend**

Ce document couvre :
- ✅ Structure standardisée `ApiResponse<T>` pour toutes les réponses
- ✅ Gestion des succès (200, 201, 204) avec exemples JSON
- ✅ Gestion des erreurs (400, 404, 500) avec `errorCode` pour i18n
- ✅ Erreurs de validation détaillées (`ValidationError[]`)
- ✅ Liste complète des ErrorCodes (1000-6999)
- ✅ Code TypeScript prêt à l'emploi (React + Axios)
- ✅ Hook custom `useApiCall()` avec gestion d'erreurs

**Points clés :**
- **success: boolean** → Toujours vérifier en premier
- **errorCode** → Utiliser pour i18n, pas `message` (en anglais)
- **validationErrors** → Afficher erreurs par champ (400 Bad Request)
- **traceId** → Logger pour support technique (500 errors)

---

### 🗄️ [REPOSITORY_PATTERN.md](REPOSITORY_PATTERN.md) - Pattern Repository Générique
**Architecture du Repository Pattern générique**

Ce document explique :
- ✅ Architecture du repository générique (`IRepository<T>`, `Repository<T>`)
- ✅ Élimination de 80% du code dupliqué
- ✅ Comment créer un nouveau repository en 3 étapes
- ✅ Override des méthodes de base
- ✅ Accès au DbContext pour requêtes complexes
- ✅ Exemples avec transactions, soft delete, etc.

**Avantages :**
- CRUD operations héritées automatiquement (GetById, GetAll, Add, Update, Delete)
- Focus sur la logique métier spécifique
- Gestion automatique des timestamps (CreatedAt, ModifiedAt)

---

## 🏗️ Architecture Générale

Pour une vue d'ensemble complète de l'architecture du projet, consultez le [README.md principal](../README.md) à la racine.

Le README principal contient :
- 📐 Architecture Clean en couches (Domain, Application, Persistence, API)
- 🚀 Guide de démarrage rapide
- 🐳 Configuration Docker
- 📝 Endpoints API disponibles
- ⚙️ Commandes Entity Framework
- 🔧 Configuration de base de données
- 📦 Structure des projets et dépendances

---

## 🎯 Guide par Scénario

### Je veux comprendre comment...

| Scénario | Document à consulter |
|----------|---------------------|
| **Écrire des tests** | [TESTING.md](TESTING.md) |
| **Gérer les erreurs** | [ERROR_HANDLING.md](ERROR_HANDLING.md) |
| **Intégrer le frontend (React/Vue/Angular)** | [API_RESPONSE_FORMAT.md](API_RESPONSE_FORMAT.md) |
| **Créer un nouveau repository** | [REPOSITORY_PATTERN.md](REPOSITORY_PATTERN.md) |
| **Ajouter une nouvelle entité** | [../README.md](../README.md) (section "Exemple d'Ajout d'une Nouvelle Entité") |
| **Comprendre l'architecture** | [../README.md](../README.md) (section "Architecture en Couches") |
| **Configurer Docker** | [../README.md](../README.md) (section "Conteneurisation avec Docker") |
| **Utiliser Entity Framework** | [../README.md](../README.md) (section "Commandes Utiles") |

---

## 📊 Vue d'Ensemble du Projet

### Technologies Principales

| Technologie | Version | Usage |
|-------------|---------|-------|
| **.NET** | 10.0 | Framework principal |
| **PostgreSQL** | 17.7 | Base de données |
| **Entity Framework Core** | 10.x | ORM |
| **FluentValidation** | 11.x | Validation des DTOs |
| **FluentResults** | 3.x | Gestion des erreurs |
| **AutoMapper** | 13.x | Mapping entités/DTOs |
| **Serilog** | Latest | Logging structuré |
| **xUnit** | 2.9.x | Framework de tests |
| **Moq** | 4.20.x | Mocking (tests unitaires) |

---

### Structure des Projets

```
DotnetApiTemplate/
├── src/
│   ├── DotnetApiTemplate.API/              → Couche présentation (Controllers, Middlewares)
│   ├── DotnetApiTemplate.Application/      → Couche application (Services, DTOs, Validators)
│   ├── DotnetApiTemplate.Domain/           → Couche domaine (Entities, Enums)
│   ├── DotnetApiTemplate.Infrastructure/   → Services externes (Email, Storage, etc.)
│   └── DotnetApiTemplate.Persistence/      → Accès données (Repositories, DbContext)
│
├── tests/
│   ├── DotnetApiTemplate.UnitTests/        → Tests unitaires (79 tests)
│   └── DotnetApiTemplate.IntegrationTests/ → Tests d'intégration (36 tests)
│
├── docs/                             → Documentation
│   ├── README.md                     → Ce fichier (index)
│   ├── TESTING.md                    → Guide des tests
│   ├── ERROR_HANDLING.md             → Gestion des erreurs
│   └── REPOSITORY_PATTERN.md         → Pattern repository
│
└── README.md                         → Documentation principale
```

---

## 🎓 Patterns et Concepts Clés

### 1. Clean Architecture
- **Domain** au centre (aucune dépendance externe)
- **Application** orchestre la logique métier
- **Persistence** implémente l'accès aux données
- **API** expose les endpoints REST
- Les dépendances pointent vers l'intérieur

### 2. CQRS Léger
- DTOs séparés pour lecture/écriture
- `CarDto` → Lecture
- `CarUpsertDto` → Création/Update complet
- `CarPatchDto` → Update partiel

### 3. Result Pattern
- Pas d'exceptions pour les erreurs métier
- Erreurs explicites dans la signature des méthodes
- Chaining et contexte préservé

### 4. Repository Pattern Générique
- CRUD operations mutualisées
- Repositories spécifiques héritent de `Repository<T>`
- Pas de code dupliqué

### 5. Dependency Injection
- Injection par constructeur
- Interfaces (contrats) dans Application
- Implémentations dans Persistence/Infrastructure

---

## 🔍 Fonctionnalités Principales

### Sécurité
- ✅ En-têtes de sécurité HTTP (SecurityHeadersMiddleware)
- ✅ CORS configurable
- ✅ Rate Limiting (100 req/min)
- ✅ HTTPS redirection
- ✅ HSTS en production

### API REST
- ✅ Versioning (v1, v2, etc.)
- ✅ Swagger/OpenAPI documentation
- ✅ Réponses standardisées (ApiResponse)
- ✅ Pagination et filtrage
- ✅ Support PATCH pour updates partiels
- ✅ Health checks

### Validation
- ✅ FluentValidation avec ErrorCodes
- ✅ ValidationFilter custom
- ✅ Validation à deux niveaux :
  - Technique (format, required) → Validators
  - Métier (business rules) → Services

### Logging
- ✅ Serilog avec enrichissement
- ✅ Logging structuré
- ✅ Request/Response logging
- ✅ Exception logging (avec filtrage des cancellations)

### Base de Données
- ✅ Migrations automatiques au démarrage
- ✅ Audit automatique (CreatedAt, ModifiedAt)
- ✅ Configurations Fluent API
- ✅ Generic Repository Pattern

### Tests
- ✅ 79 tests unitaires (Validators, Services)
- ✅ 36 tests d'intégration (Controllers, Repositories)
- ✅ WebApplicationFactory pour tests HTTP
- ✅ InMemory Database pour tests d'intégration
- ✅ FluentAssertions pour assertions lisibles

---

## 🚀 Commandes Rapides

### Développement

```bash
# Lancer l'API en mode développement
dotnet run --project src/DotnetApiTemplate.API

# Watch mode (rechargement automatique)
dotnet watch run --project src/DotnetApiTemplate.API

# Lancer avec Docker
docker-compose up -d
```

### Tests

```bash
# Tous les tests
dotnet test

# Tests unitaires uniquement
dotnet test tests/DotnetApiTemplate.UnitTests

# Tests d'intégration uniquement
dotnet test tests/DotnetApiTemplate.IntegrationTests

# Avec verbosité détaillée
dotnet test --logger "console;verbosity=detailed"
```

### Entity Framework

```bash
# Créer une migration
dotnet ef migrations add NomMigration --project src/DotnetApiTemplate.Persistence --startup-project src/DotnetApiTemplate.API

# Appliquer les migrations
dotnet ef database update --project src/DotnetApiTemplate.Persistence --startup-project src/DotnetApiTemplate.API

# Supprimer la dernière migration
dotnet ef migrations remove --project src/DotnetApiTemplate.Persistence --startup-project src/DotnetApiTemplate.API
```

### Docker

```bash
# Démarrer tous les services
docker-compose up -d

# Voir les logs
docker-compose logs -f webapi

# Rebuild après modifications
docker-compose up -d --build webapi

# Arrêter et supprimer volumes
docker-compose down -v
```

---

## 🎯 Bonnes Pratiques

### Code
- ✅ Un fichier = Une classe/interface
- ✅ Nommage clair et explicite (pas d'abréviations)
- ✅ Async/Await pour toutes les opérations I/O
- ✅ Suffixer les méthodes async avec `Async`
- ✅ XML comments pour les APIs publiques

### Architecture
- ✅ Respecter les dépendances unidirectionnelles
- ✅ Pas de logique métier dans Controllers/Repositories
- ✅ Utiliser les interfaces (DIP - Dependency Inversion Principle)
- ✅ DTOs pour exposer les données (jamais les entités)

### Validation
- ✅ FluentValidation pour validation technique
- ✅ Services pour validation métier
- ✅ ErrorCodes pour i18n côté frontend

### Erreurs
- ✅ Result Pattern pour erreurs métier
- ✅ Exceptions pour erreurs système
- ✅ Jamais d'exceptions pour le contrôle de flux

### Tests
- ✅ AAA Pattern (Arrange, Act, Assert)
- ✅ Noms descriptifs : `Should_ReturnError_When_MileageDecreases`
- ✅ Un assert par test (sauf intégration)
- ✅ Tests isolés et déterministes

---

## 📞 Support et Ressources

### Documentation Officielle

- [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [FluentResults](https://github.com/altmann/FluentResults)
- [AutoMapper](https://docs.automapper.org/)
- [xUnit](https://xunit.net/)

### Concepts et Patterns

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design)
- [Result Pattern / Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)

---

## 📝 Notes de Version

### Version Actuelle : 1.0.0

**Fonctionnalités incluses :**
- ✅ Clean Architecture (4 couches)
- ✅ Generic Repository Pattern
- ✅ Result Pattern (FluentResults)
- ✅ FluentValidation avec ErrorCodes
- ✅ 115 tests (79 unitaires + 36 intégration)
- ✅ Swagger/OpenAPI
- ✅ Docker support complet
- ✅ Serilog logging
- ✅ Health checks
- ✅ Rate limiting
- ✅ CORS
- ✅ API versioning

---

## 🤝 Contribution

Ce template est conçu pour être un point de départ solide pour vos projets ASP.NET Core. N'hésitez pas à l'adapter selon vos besoins spécifiques.

### Suggestions d'Améliorations

Quelques idées pour étendre ce template :
- 🔐 Authentification JWT/OAuth
- 📧 Service d'email (dans Infrastructure)
- 📦 Upload de fichiers vers S3/Azure Blob
- 🔄 CQRS complet avec MediatR
- 📊 Application Insights / Observabilité
- 🌍 Localisation (i18n)
- 🔄 Idempotence pour APIs
- 📝 Audit trail complet

---

**Dernière mise à jour** : Janvier 2026

---

**Navigation rapide** : [Tests](TESTING.md) | [Erreurs](ERROR_HANDLING.md) | [Repositories](REPOSITORY_PATTERN.md) | [README Principal](../README.md)
