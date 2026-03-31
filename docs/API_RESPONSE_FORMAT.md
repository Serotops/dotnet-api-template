# Format des Réponses API - Guide Frontend

## Vue d'Ensemble

Toutes les réponses de l'API (sauf `/health`, `/swagger`, et les `204 No Content`) sont encapsulées dans un format standardisé `ApiResponse<T>`. Ce format uniforme facilite le traitement des succès et des erreurs côté frontend.

---

## Structure TypeScript

```typescript
interface ApiResponse<T> {
  success: boolean;           // true = succès, false = erreur
  data?: T;                   // Données en cas de succès (type générique)
  message?: string;           // Message optionnel (succès ou erreur)
  errorCode?: string;         // Code d'erreur pour i18n (enum ErrorCode)
  errors?: string[];          // Liste d'erreurs générales
  validationErrors?: ValidationError[];  // Erreurs de validation détaillées
  traceId?: string;           // ID de traçage pour debugging (en cas d'erreur)
}

interface ValidationError {
  field: string;              // Nom du champ (ex: "Make", "Year")
  message: string;            // Message d'erreur en anglais
  errorCode?: string;         // Code d'erreur spécifique pour i18n
  attemptedValue?: any;       // Valeur soumise qui a échoué
}
```

---

## Codes HTTP Utilisés

| Code HTTP | Signification | Cas d'Usage |
|-----------|---------------|-------------|
| **200 OK** | Succès | GET, PUT, PATCH réussis |
| **201 Created** | Ressource créée | POST réussi |
| **204 No Content** | Succès sans body | DELETE réussi (pas de ApiResponse) |
| **400 Bad Request** | Erreur client | Validation échouée, business logic |
| **404 Not Found** | Ressource inexistante | GET/PUT/DELETE sur ID inexistant |
| **500 Internal Server Error** | Erreur serveur | Exception non gérée |

---

## 1. Réponses de Succès

### 1.1 GET - Récupération d'une ressource

**Requête:**
```http
GET /api/v1/cars/123e4567-e89b-12d3-a456-426614174000
```

**Réponse: 200 OK**
```json
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "make": "Toyota",
    "model": "Corolla",
    "year": 2021,
    "color": "White",
    "price": 25000,
    "vin": "1HGBH41JXMN109186",
    "mileage": 30000,
    "isAvailable": true,
    "createdAt": "2024-01-15T10:30:00Z",
    "modifiedAt": "2024-01-15T10:30:00Z"
  },
  "message": null,
  "errorCode": null,
  "errors": null,
  "validationErrors": null,
  "traceId": null
}
```

### 1.2 GET - Liste avec pagination

**Requête:**
```http
GET /api/v1/cars?pageIndex=1&pageSize=10
```

**Réponse: 200 OK**
```json
{
  "success": true,
  "data": {
    "data": [
      {
        "id": "123e4567-e89b-12d3-a456-426614174000",
        "make": "Toyota",
        "model": "Corolla",
        "year": 2021,
        "color": "White",
        "price": 25000,
        "mileage": 30000,
        "isAvailable": true
      },
      {
        "id": "223e4567-e89b-12d3-a456-426614174001",
        "make": "Honda",
        "model": "Civic",
        "year": 2022,
        "color": "Blue",
        "price": 28000,
        "mileage": 15000,
        "isAvailable": true
      }
    ],
    "pageIndex": 1,
    "pageSize": 10,
    "totalItems": 25,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "errorCode": null,
  "errors": null,
  "validationErrors": null,
  "traceId": null
}
```

### 1.3 POST - Création réussie

**Requête:**
```http
POST /api/v1/cars
Content-Type: application/json

{
  "make": "Renault",
  "model": "Clio",
  "year": 2020,
  "color": "Black",
  "price": 22000,
  "vin": "12345678901234567",
  "mileage": 62000,
  "isAvailable": true
}
```

**Réponse: 201 Created**
```json
{
  "success": true,
  "data": {
    "id": "323e4567-e89b-12d3-a456-426614174002",
    "make": "Renault",
    "model": "Clio",
    "year": 2020,
    "color": "Black",
    "price": 22000,
    "vin": "12345678901234567",
    "mileage": 62000,
    "isAvailable": true,
    "createdAt": "2024-01-15T14:25:00Z",
    "modifiedAt": "2024-01-15T14:25:00Z"
  },
  "message": null,
  "errorCode": null,
  "errors": null,
  "validationErrors": null,
  "traceId": null
}
```

**Header:**
```
Location: /api/v1/cars/323e4567-e89b-12d3-a456-426614174002
```

### 1.4 PUT/PATCH - Mise à jour réussie

**Réponse: 200 OK**
```json
{
  "success": true,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "make": "Toyota",
    "model": "Corolla",
    "year": 2021,
    "color": "Red",  // Changé
    "price": 24000,  // Changé
    "mileage": 32000,
    "isAvailable": true,
    "createdAt": "2024-01-15T10:30:00Z",
    "modifiedAt": "2024-01-16T09:15:00Z"  // Mis à jour
  },
  "message": null,
  "errorCode": null,
  "errors": null,
  "validationErrors": null,
  "traceId": null
}
```

### 1.5 DELETE - Suppression réussie

**Réponse: 204 No Content**
- ⚠️ Pas de body (pas de `ApiResponse`)
- Vérifier simplement `response.status === 204`

---

## 2. Réponses d'Erreur

### 2.1 Erreurs de Validation (400 Bad Request)

**Requête invalide:**
```http
POST /api/v1/cars
Content-Type: application/json

{
  "make": "",           // ❌ Vide
  "model": "",          // ❌ Vide
  "year": 1800,         // ❌ < 1900
  "color": "",          // ❌ Vide
  "price": -100,        // ❌ Négatif
  "mileage": -50        // ❌ Négatif
}
```

**Réponse: 400 Bad Request**
```json
{
  "success": false,
  "data": null,
  "message": "One or more validation errors occurred",
  "errorCode": "VALIDATION_ERROR",
  "errors": null,
  "validationErrors": [
    {
      "field": "Make",
      "message": "Make is required",
      "errorCode": "REQUIRED_FIELD_MISSING",
      "attemptedValue": ""
    },
    {
      "field": "Model",
      "message": "Model is required",
      "errorCode": "REQUIRED_FIELD_MISSING",
      "attemptedValue": ""
    },
    {
      "field": "Year",
      "message": "Year must be between 1900 and 2100",
      "errorCode": "INVALID_FIELD_RANGE",
      "attemptedValue": 1800
    },
    {
      "field": "Color",
      "message": "Color is required",
      "errorCode": "REQUIRED_FIELD_MISSING",
      "attemptedValue": ""
    },
    {
      "field": "Price",
      "message": "Price must be greater than 0",
      "errorCode": "INVALID_PRICE",
      "attemptedValue": -100
    },
    {
      "field": "Mileage",
      "message": "Mileage must be greater than or equal to 0",
      "errorCode": "INVALID_MILEAGE",
      "attemptedValue": -50
    }
  ],
  "traceId": null
}
```

**Comment traiter:**
```typescript
if (response.status === 400 && response.data.validationErrors) {
  // Afficher les erreurs par champ
  response.data.validationErrors.forEach(error => {
    // Traduire le message avec error.errorCode
    const translatedMessage = i18n.t(`errors.${error.errorCode}`);
    showFieldError(error.field, translatedMessage);
  });
}
```

### 2.2 Ressource Non Trouvée (404 Not Found)

**Requête:**
```http
GET /api/v1/cars/99999999-9999-9999-9999-999999999999
```

**Réponse: 404 Not Found**
```json
{
  "success": false,
  "data": null,
  "message": "Car with ID 99999999-9999-9999-9999-999999999999 was not found",
  "errorCode": "CAR_NOT_FOUND",
  "errors": [
    "Car with ID 99999999-9999-9999-9999-999999999999 was not found"
  ],
  "validationErrors": null,
  "traceId": null
}
```

**Comment traiter:**
```typescript
if (response.status === 404) {
  const message = i18n.t(`errors.${response.data.errorCode}`);
  showNotification('error', message);
  // Rediriger vers la liste ou afficher une page 404
}
```

### 2.3 Erreur de Logique Métier (400 Bad Request)

**Exemple: Kilométrage qui diminue**
```http
PUT /api/v1/cars/123e4567-e89b-12d3-a456-426614174000
Content-Type: application/json

{
  "make": "Toyota",
  "model": "Corolla",
  "year": 2021,
  "color": "White",
  "price": 25000,
  "mileage": 10000  // ❌ Était 30000 avant (diminution impossible)
}
```

**Réponse: 400 Bad Request**
```json
{
  "success": false,
  "data": null,
  "message": "Mileage cannot decrease. Current: 30000, Attempted: 10000",
  "errorCode": "INVALID_MILEAGE",
  "errors": [
    "Mileage cannot decrease. Current: 30000, Attempted: 10000"
  ],
  "validationErrors": null,
  "traceId": null
}
```

**Comment traiter:**
```typescript
if (response.status === 400 && !response.data.validationErrors) {
  // Erreur de business logic
  const message = i18n.t(`errors.${response.data.errorCode}`, {
    // Passer des paramètres dynamiques si nécessaire
    current: 30000,
    attempted: 10000
  });
  showNotification('error', message);
}
```

### 2.4 Erreur Serveur (500 Internal Server Error)

**Réponse: 500 Internal Server Error**
```json
{
  "success": false,
  "data": null,
  "message": "An unexpected error occurred",
  "errorCode": "INTERNAL_SERVER_ERROR",
  "errors": [
    "An unexpected error occurred. Please try again later."
  ],
  "validationErrors": null,
  "traceId": "0HNIIO0L18OOU"  // Pour le support technique
}
```

**Comment traiter:**
```typescript
if (response.status === 500) {
  const message = i18n.t('errors.INTERNAL_SERVER_ERROR');
  showNotification('error', message);
  // Logger le traceId pour le support
  logError({
    traceId: response.data.traceId,
    endpoint: '/api/v1/cars',
    timestamp: new Date()
  });
}
```

---

## 3. Liste Complète des ErrorCodes

### Erreurs Générales (1000-1999)
```typescript
enum GeneralErrorCode {
  UNKNOWN_ERROR = 1000,
  INTERNAL_SERVER_ERROR = 1001,
  BAD_REQUEST = 1002,
  UNAUTHORIZED = 1003,
  FORBIDDEN = 1004,
  SERVICE_UNAVAILABLE = 1005
}
```

### Erreurs Ressources (2000-2999)
```typescript
enum ResourceErrorCode {
  RESOURCE_NOT_FOUND = 2000,
  RESOURCE_ALREADY_EXISTS = 2001,
  RESOURCE_DELETED = 2002
}
```

### Erreurs Validation (3000-3999)
```typescript
enum ValidationErrorCode {
  VALIDATION_ERROR = 3000,
  REQUIRED_FIELD_MISSING = 3001,
  INVALID_FIELD_FORMAT = 3002,
  INVALID_FIELD_LENGTH = 3003,
  INVALID_FIELD_RANGE = 3004
}
```

### Erreurs Métier - Cars (4000-4999)
```typescript
enum CarErrorCode {
  CAR_NOT_FOUND = 4000,
  CAR_ALREADY_EXISTS = 4001,
  CAR_NOT_AVAILABLE = 4002,
  INVALID_VIN = 4003,
  INVALID_YEAR = 4004,
  INVALID_PRICE = 4005,
  INVALID_MILEAGE = 4006
}
```

### Erreurs Base de Données (5000-5999)
```typescript
enum DatabaseErrorCode {
  DATABASE_ERROR = 5000,
  DATABASE_CONNECTION_FAILED = 5001,
  DUPLICATE_ENTRY = 5002,
  CONSTRAINT_VIOLATION = 5003
}
```

### Erreurs Auth (6000-6999)
```typescript
enum AuthErrorCode {
  INVALID_CREDENTIALS = 6000,
  TOKEN_EXPIRED = 6001,
  TOKEN_INVALID = 6002,
  INSUFFICIENT_PERMISSIONS = 6003
}
```

---

## 4. Gestion des Erreurs - Code Frontend

### 4.1 Fonction Utilitaire Générique

```typescript
interface ApiError {
  status: number;
  errorCode: string;
  message: string;
  validationErrors?: ValidationError[];
  errors?: string[];
  traceId?: string;
}

async function handleApiCall<T>(
  apiCall: () => Promise<AxiosResponse<ApiResponse<T>>>
): Promise<T> {
  try {
    const response = await apiCall();

    // Vérifier le succès dans le body (même si status 200)
    if (response.data.success) {
      return response.data.data!;
    }

    // Cas où success=false mais status 2xx (ne devrait pas arriver)
    throw createApiError(response);

  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      throw createApiError(error.response);
    }
    // Erreur réseau ou autre
    throw {
      status: 0,
      errorCode: 'NETWORK_ERROR',
      message: 'Network error. Please check your connection.',
      validationErrors: undefined,
      errors: undefined,
      traceId: undefined
    } as ApiError;
  }
}

function createApiError(response: AxiosResponse<ApiResponse<any>>): ApiError {
  const data = response.data;
  return {
    status: response.status,
    errorCode: data.errorCode || 'UNKNOWN_ERROR',
    message: data.message || 'An error occurred',
    validationErrors: data.validationErrors,
    errors: data.errors,
    traceId: data.traceId
  };
}
```

### 4.2 Utilisation dans un Composant

```typescript
// GET
async function fetchCar(id: string): Promise<Car> {
  return handleApiCall(() =>
    axios.get<ApiResponse<Car>>(`/api/v1/cars/${id}`)
  );
}

// POST
async function createCar(car: CarUpsertDto): Promise<Car> {
  return handleApiCall(() =>
    axios.post<ApiResponse<Car>>('/api/v1/cars', car)
  );
}

// Usage dans le composant
try {
  const car = await fetchCar('123e4567-e89b-12d3-a456-426614174000');
  console.log('Car fetched:', car);

} catch (error: any) {
  const apiError = error as ApiError;

  // Gestion par type d'erreur
  switch (apiError.status) {
    case 400:
      if (apiError.validationErrors) {
        // Erreurs de validation
        apiError.validationErrors.forEach(validationError => {
          const message = i18n.t(`errors.${validationError.errorCode}`, {
            field: validationError.field,
            value: validationError.attemptedValue
          });
          setFieldError(validationError.field, message);
        });
      } else {
        // Erreur de business logic
        showNotification('error', i18n.t(`errors.${apiError.errorCode}`));
      }
      break;

    case 404:
      showNotification('error', i18n.t('errors.CAR_NOT_FOUND'));
      navigate('/cars');
      break;

    case 500:
      showNotification('error', i18n.t('errors.INTERNAL_SERVER_ERROR'));
      logErrorToMonitoring({
        traceId: apiError.traceId,
        endpoint: '/api/v1/cars',
        errorCode: apiError.errorCode
      });
      break;

    default:
      showNotification('error', i18n.t('errors.UNKNOWN_ERROR'));
  }
}
```

### 4.3 Fichier i18n (Exemple en français)

```json
{
  "errors": {
    "INTERNAL_SERVER_ERROR": "Une erreur inattendue s'est produite. Veuillez réessayer.",
    "CAR_NOT_FOUND": "La voiture demandée n'existe pas.",
    "VALIDATION_ERROR": "Veuillez corriger les erreurs de saisie.",
    "REQUIRED_FIELD_MISSING": "Ce champ est obligatoire.",
    "INVALID_FIELD_RANGE": "La valeur doit être dans la plage autorisée.",
    "INVALID_PRICE": "Le prix doit être positif.",
    "INVALID_MILEAGE": "Le kilométrage ne peut pas diminuer.",
    "INVALID_YEAR": "L'année doit être entre 1900 et 2100.",
    "NETWORK_ERROR": "Erreur réseau. Vérifiez votre connexion."
  }
}
```

---

## 5. Checklist Frontend

### ✅ À Faire Systématiquement

1. **Toujours vérifier `success: boolean`** dans le body, pas seulement le status HTTP
2. **Gérer les `validationErrors`** pour afficher les erreurs par champ
3. **Utiliser `errorCode`** pour l'internationalisation (i18n), pas `message`
4. **Logger `traceId`** en cas d'erreur 500 pour le support
5. **Gérer les 204 No Content** sans body (DELETE)
6. **Tester les cas d'erreur** en plus des cas de succès

### ⚠️ Pièges à Éviter

- ❌ Ne pas se fier uniquement au status HTTP (toujours vérifier `success`)
- ❌ Ne pas afficher directement `message` (en anglais) → utiliser `errorCode` + i18n
- ❌ Ne pas ignorer `validationErrors` pour les 400 Bad Request
- ❌ Ne pas oublier de gérer les erreurs réseau (timeout, connexion)
- ❌ Ne pas faire confiance au type de `data` sans vérifier `success` d'abord

---

## 6. Exemple Complet React + TypeScript

```typescript
import axios, { AxiosResponse } from 'axios';
import { useTranslation } from 'react-i18next';

// Types
interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errorCode?: string;
  errors?: string[];
  validationErrors?: ValidationError[];
  traceId?: string;
}

interface ValidationError {
  field: string;
  message: string;
  errorCode?: string;
  attemptedValue?: any;
}

interface Car {
  id: string;
  make: string;
  model: string;
  year: number;
  color: string;
  price: number;
  mileage: number;
  isAvailable: boolean;
}

// Hook custom pour appels API
function useApiCall() {
  const { t } = useTranslation();

  const call = async <T,>(
    apiCall: () => Promise<AxiosResponse<ApiResponse<T>>>
  ): Promise<T> => {
    try {
      const response = await apiCall();

      if (response.data.success && response.data.data) {
        return response.data.data;
      }

      throw new Error('API returned success=false');

    } catch (error: any) {
      if (axios.isAxiosError(error) && error.response) {
        const apiResponse = error.response.data as ApiResponse<any>;

        // Construire un message d'erreur utilisable
        let errorMessage = t(`errors.${apiResponse.errorCode || 'UNKNOWN_ERROR'}`);

        // Si erreurs de validation, les combiner
        if (apiResponse.validationErrors && apiResponse.validationErrors.length > 0) {
          errorMessage = apiResponse.validationErrors
            .map(ve => `${ve.field}: ${t(`errors.${ve.errorCode || 'VALIDATION_ERROR'}`)}`)
            .join('\n');
        }

        throw new Error(errorMessage);
      }

      throw new Error(t('errors.NETWORK_ERROR'));
    }
  };

  return { call };
}

// Composant exemple
function CarDetails({ carId }: { carId: string }) {
  const [car, setCar] = useState<Car | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { call } = useApiCall();

  const loadCar = async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await call<Car>(() =>
        axios.get(`/api/v1/cars/${carId}`)
      );
      setCar(data);

    } catch (err: any) {
      setError(err.message);

    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCar();
  }, [carId]);

  if (loading) return <div>Loading...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!car) return <div>No car found</div>;

  return (
    <div>
      <h1>{car.make} {car.model}</h1>
      <p>Year: {car.year}</p>
      <p>Price: ${car.price}</p>
      <p>Mileage: {car.mileage} km</p>
    </div>
  );
}
```

---

## 7. Résumé Rapide

### Structure de base
```typescript
{
  success: boolean,      // ✅ Toujours vérifier ça en premier
  data?: T,              // Données si succès
  errorCode?: string,    // 🌍 Utiliser pour i18n
  validationErrors?: [], // 📝 Erreurs par champ
  errors?: [],           // 📋 Erreurs générales
  traceId?: string       // 🔍 Pour support technique
}
```

### Flow de traitement
```
1. Vérifier response.data.success === true
   ├─ true  → Utiliser response.data.data
   └─ false → Gérer l'erreur avec errorCode

2. Si erreur, check validationErrors
   ├─ Présent  → Afficher erreurs par champ
   └─ Absent   → Afficher erreur générale

3. Toujours traduire avec errorCode, jamais message brut
```

**Prêt à intégrer dans ton frontend !** 🚀
