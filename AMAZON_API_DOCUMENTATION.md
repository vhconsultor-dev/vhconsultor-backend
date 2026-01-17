# Amazon SP-API - Documentación

## Descripción General

Este módulo proporciona integración con Amazon Selling Partner API (SP-API) para gestionar tokens de acceso necesarios para interactuar con las APIs de Amazon Seller y Vendor.

## Estructura de Carpetas

```
vhconsultor-backend/
├── ModelLayer/
│   └── Amazon/
│       └── Entities/
│           └── AmazonToken.cs
├── BusinessLayer/
│   └── Amazon/
│       ├── AmazonSettings.cs
│       ├── Commands/
│       │   ├── GenerateAccessTokenCommand.cs
│       │   └── AmazonTokenCommandRepository.cs
│       ├── Queries/
│       │   └── AmazonTokenQueryRepository.cs
│       └── Validators/
│           └── GenerateAccessTokenValidator.cs
├── ApplicationLayer/
│   └── Amazon/
│       └── AmazonAuthService.cs
├── ApiLayer/
│   └── Controllers/
│       └── Amazon/
│           └── AmazonAuthController.cs
└── Scripts/
    └── CreateAmazonTokensTable.sql
```

## Configuración

### 1. Configuración en appsettings.json

```json
{
  "Amazon": {
    "ClientId": "amzn1.application-oa2-client.YOUR_CLIENT_ID",
    "ClientSecret": "amzn1.oa2-cs.YOUR_CLIENT_SECRET",
    "RefreshToken": "Atzr|YOUR_REFRESH_TOKEN",
    "TokenEndpoint": "https://api.amazon.com/auth/o2/token",
    "GrantType": "refresh_token"
  }
}
```

### 2. Crear la tabla en la base de datos

Ejecutar el script SQL ubicado en:
```
Scripts/CreateAmazonTokensTable.sql
```

Este script creará:
- El schema `Amazon`
- La tabla `AmazonTokens`
- Índices para optimizar las consultas

## Endpoints de la API

### Base URL
```
/api/amazon/amazonauth
```

---

## 1. Generar Access Token

**Endpoint:** `POST /api/amazon/amazonauth/generate-token`

**Descripción:** Genera un nuevo access token usando el refresh token de Amazon. El token tiene una duración de 1 hora.

**Autorización:** No requiere (AllowAnonymous)

### Request Body

```json
{
  "refreshToken": "YOUR_AMAZON_REFRESH_TOKEN",
  "clientId": "YOUR_AMAZON_CLIENT_ID",
  "clientSecret": "YOUR_AMAZON_CLIENT_SECRET"
}
```

### Response (Success - 200 OK)

```json
{
  "success": true,
  "message": "Access token generado exitosamente",
  "data": {
    "tokenId": 1,
    "accessToken": "Atza|IwEBIJ7JdF7...",
    "refreshToken": "Atzr|IwEBIFQVzXIt...",
    "tokenType": "bearer",
    "expiresIn": 3600,
    "expiresAt": "2026-01-17T15:30:00",
    "message": "Access token generado exitosamente"
  }
}
```

### Response (Error - 400 Bad Request)

```json
{
  "success": false,
  "message": "Error al obtener el token de Amazon: BadRequest - invalid_grant",
  "data": null
}
```

### Validaciones

- `refreshToken`: Requerido, mínimo 50 caracteres
- `clientId`: Requerido, debe comenzar con `amzn1.application-oa2-client.`
- `clientSecret`: Requerido, debe comenzar con `amzn1.oa2-cs.`

---

## 2. Obtener Token Activo

**Endpoint:** `GET /api/amazon/amazonauth/active-token`

**Descripción:** Obtiene el token activo más reciente que no ha expirado.

**Autorización:** Requiere Bearer Token

### Response (Success - 200 OK)

```json
{
  "success": true,
  "message": "Token activo obtenido exitosamente",
  "data": {
    "tokenId": 1,
    "accessToken": "Atza|IwEBIJ7JdF7...",
    "tokenType": "bearer",
    "expiresIn": 3600,
    "createdAt": "2026-01-17T14:30:00",
    "expiresAt": "2026-01-17T15:30:00",
    "clientId": "YOUR_AMAZON_CLIENT_ID"
  }
}
```

### Response (Not Found - 404)

```json
{
  "success": false,
  "message": "No hay tokens activos disponibles",
  "data": null
}
```

---

## 3. Obtener Tokens con Filtros

**Endpoint:** `GET /api/amazon/amazonauth/tokens`

**Descripción:** Obtiene una lista de tokens con filtros opcionales.

**Autorización:** Requiere Bearer Token

### Query Parameters

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| tokenId | int | No | ID específico del token |
| clientId | string | No | Client ID de Amazon |
| isActive | bool | No | Filtrar por estado activo |
| createdFrom | DateTime | No | Fecha de creación desde |
| createdTo | DateTime | No | Fecha de creación hasta |
| limit | int | No | Límite de registros (default: 50) |

### Ejemplo de Request

```
GET /api/amazon/amazonauth/tokens?isActive=true&limit=10
```

### Response (Success - 200 OK)

```json
{
  "success": true,
  "message": "2 token(s) encontrado(s)",
  "data": [
    {
      "tokenId": 2,
      "accessToken": "Atza|IwEBIJ7JdF7...",
      "tokenType": "bearer",
      "expiresIn": 3600,
      "createdAt": "2026-01-17T14:30:00",
      "expiresAt": "2026-01-17T15:30:00",
      "isActive": true,
      "clientId": "YOUR_AMAZON_CLIENT_ID",
      "notes": "Token generado automáticamente"
    },
    {
      "tokenId": 1,
      "accessToken": "Atza|IwEBIJ7JdF7...",
      "tokenType": "bearer",
      "expiresIn": 3600,
      "createdAt": "2026-01-17T13:30:00",
      "expiresAt": "2026-01-17T14:30:00",
      "isActive": true,
      "clientId": "YOUR_AMAZON_CLIENT_ID",
      "notes": "Token generado automáticamente"
    }
  ]
}
```

---

## 4. Desactivar Tokens Expirados

**Endpoint:** `POST /api/amazon/amazonauth/deactivate-expired`

**Descripción:** Desactiva todos los tokens que ya han expirado.

**Autorización:** Requiere Bearer Token

### Response (Success - 200 OK)

```json
{
  "success": true,
  "message": "3 token(s) desactivado(s)",
  "data": {
    "tokensDeactivated": 3
  }
}
```

---

## Modelo de Datos

### AmazonToken

```csharp
public class AmazonToken
{
    public int TokenId { get; set; }
    public string RefreshToken { get; set; }
    public string AccessToken { get; set; }
    public string TokenType { get; set; }        // Default: "bearer"
    public int ExpiresIn { get; set; }           // En segundos (3600 = 1 hora)
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? ClientId { get; set; }
    public string? Notes { get; set; }
}
```

---

## Flujo de Trabajo Recomendado

### 1. Generación Inicial del Token

```bash
POST /api/amazon/amazonauth/generate-token
```

Con los datos de Amazon (RefreshToken, ClientId, ClientSecret).

### 2. Uso del Token

Cuando necesites hacer llamadas a Amazon SP-API:

```bash
GET /api/amazon/amazonauth/active-token
```

Usa el `accessToken` devuelto en tus peticiones a Amazon.

### 3. Renovación Automática

Como los tokens expiran en 1 hora, puedes:

1. **Opción A:** Verificar si el token está próximo a expirar y generar uno nuevo
2. **Opción B:** Generar un nuevo token cada vez que lo necesites (Amazon permite esto)
3. **Opción C:** Implementar un job que genere tokens nuevos cada 50 minutos

### 4. Limpieza de Tokens Expirados

Periódicamente ejecutar:

```bash
POST /api/amazon/amazonauth/deactivate-expired
```

---

## Consideraciones de Seguridad

1. **Protección de Credenciales:**
   - Nunca expongas el `ClientSecret` en el frontend
   - Usa variables de entorno en producción
   - El endpoint de generación está sin autenticación para facilitar pruebas, pero deberías protegerlo en producción

2. **Almacenamiento de Tokens:**
   - Los tokens se almacenan en la base de datos
   - Solo el token activo más reciente es válido
   - Los tokens antiguos se desactivan automáticamente

3. **Gestión de Refresh Token:**
   - El Refresh Token de Amazon no expira
   - Guárdalo de forma segura
   - Si se compromete, debes revocarlo desde Amazon Seller Central

---

## Próximos Pasos

Una vez que tengas el access token, puedes:

1. **Seller API:**
   - Obtener órdenes
   - Gestionar inventario
   - Consultar reportes de ventas

2. **Vendor API:**
   - Órdenes de compra
   - Facturas
   - Envíos

3. **Implementar más endpoints:**
   - Crear carpetas para `Seller` y `Vendor` dentro de `Amazon`
   - Cada uno con sus propios servicios y controladores

---

## Ejemplo de Uso con Postman

### 1. Generar Token

```
POST https://localhost:5001/api/amazon/amazonauth/generate-token
Content-Type: application/json

{
  "refreshToken": "Atzr|...",
  "clientId": "amzn1.application-oa2-client...",
  "clientSecret": "amzn1.oa2-cs..."
}
```

### 2. Obtener Token Activo

```
GET https://localhost:5001/api/amazon/amazonauth/active-token
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Troubleshooting

### Error: "invalid_grant"
- El refresh token ha expirado o es inválido
- Verifica que el refresh token sea correcto
- Genera un nuevo refresh token desde Amazon Seller Central

### Error: "invalid_client"
- El Client ID o Client Secret son incorrectos
- Verifica las credenciales en Amazon Seller Central

### Error: "No hay tokens activos disponibles"
- No hay tokens generados o todos han expirado
- Genera un nuevo token con el endpoint POST

---

## Referencias

- [Amazon SP-API Documentation](https://developer-docs.amazon.com/sp-api/)
- [Amazon OAuth 2.0 Documentation](https://developer.amazon.com/docs/login-with-amazon/authorization-code-grant.html)
