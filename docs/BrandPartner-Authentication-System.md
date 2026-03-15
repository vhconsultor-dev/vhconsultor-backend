# Sistema de Autenticación Brand Partner - Documentación Completa

## Descripción General

Se ha implementado un sistema completo de autenticación con 2FA (Two-Factor Authentication) para usuarios Brand Partner. Este sistema es independiente del sistema de autenticación Corporate y está diseñado para clientes que necesitan acceso a la plataforma.

---

## Estructura de Base de Datos

### Schema: `BrandPartner`

#### Tabla: `BrandPartner.BrandPartnerUsers`

**Descripción:** Almacena los usuarios Brand Partner vinculados a clientes (Customers).

**Columnas:**
- `BrandPartnerUserId` (INT, PK, IDENTITY) - ID único del usuario
- `CustomerId` (INT, FK a `Corporate.Customers`, NOT NULL) - Cliente al que pertenece
- `Email` (NVARCHAR(255), NOT NULL) - Email de login (único por Customer)
- `PasswordHash` (NVARCHAR(500), NOT NULL) - Hash SHA256 de la contraseña
- `FirstName` (NVARCHAR(100), NOT NULL) - Nombre del usuario
- `LastName` (NVARCHAR(100), NOT NULL) - Apellido del usuario
- `PhoneNumber` (NVARCHAR(50), NULL) - Teléfono de contacto
- `IsActive` (BIT, NOT NULL, DEFAULT 1) - Usuario activo/inactivo
- `EmailVerified` (BIT, NOT NULL, DEFAULT 0) - Email verificado
- `RequirePasswordChangeOnNextLogin` (BIT, NOT NULL, DEFAULT 0) - Forzar cambio de contraseña
- `FailedLoginAttempts` (INT, NOT NULL, DEFAULT 0) - Intentos fallidos de login
- `LockedUntil` (DATETIME2, NULL) - Fecha hasta la cual está bloqueado (30 min después de 5 intentos)
- `LastLogin` (DATETIME2, NULL) - Último login exitoso
- `LastLoginIP` (NVARCHAR(45), NULL) - IP del último login
- `LastLoginUserAgent` (NVARCHAR(500), NULL) - User-Agent del último login
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT GETUTCDATE())
- `UpdatedAt` (DATETIME2, NULL)
- `CreatedBy` (NVARCHAR(255), NULL)

**Índices:**
- `UNIQUE (CustomerId, Email)` - Un email por cliente
- `IX_CustomerId` - Búsqueda por cliente
- `IX_Email` - Búsqueda por email (para login)

---

#### Tabla: `BrandPartner.BrandPartnerUserLoginHistory`

**Descripción:** Historial de intentos de login (exitosos y fallidos).

**Columnas:**
- `LoginHistoryId` (INT, PK, IDENTITY)
- `BrandPartnerUserId` (INT, FK, NOT NULL)
- `LoginDate` (DATETIME2, NOT NULL, DEFAULT GETUTCDATE())
- `IPAddress` (NVARCHAR(45), NOT NULL)
- `UserAgent` (NVARCHAR(500), NULL)
- `Location` (NVARCHAR(255), NULL)
- `Country` (NVARCHAR(100), NULL)
- `City` (NVARCHAR(100), NULL)
- `LoginSuccessful` (BIT, NOT NULL)
- `FailureReason` (NVARCHAR(255), NULL)
- `SessionId` (NVARCHAR(255), NULL) - ID de sesión JWT

**Índices:**
- `IX_BrandPartnerUserId`
- `IX_LoginDate`

---

#### Tabla: `BrandPartner.BrandPartnerTwoFactorCodes`

**Descripción:** Códigos 2FA de un solo uso para verificación de login.

**Columnas:**
- `TwoFactorCodeId` (INT, PK, IDENTITY)
- `BrandPartnerUserId` (INT, FK, NOT NULL)
- `Code` (NVARCHAR(5), NOT NULL) - Formato: 1 letra mayúscula A-Z + 4 dígitos (ej: A1234)
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT GETUTCDATE())
- `ExpiresAt` (DATETIME2, NOT NULL) - 3 minutos desde `CreatedAt`
- `IsUsed` (BIT, NOT NULL, DEFAULT 0)
- `UsedAt` (DATETIME2, NULL)

**Índices:**
- `IX_BrandPartnerUserId`
- `IX_ExpiresAt` (WHERE `IsUsed = 0`) - Para búsqueda de códigos activos

---

## API Endpoints

### Base URL: `/api/brandpartner/auth`

### 1. **POST** `/api/brandpartner/auth/users`
**Autenticación:** Requiere `[Authorize(Roles = "Admin,Corporate")]`

**Descripción:** Crea un nuevo usuario Brand Partner.

**Request Body:**
```json
{
  "customerId": 1,
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "Juan",
  "lastName": "Pérez",
  "phoneNumber": "+506 8888-8888",
  "createdBy": "admin@vhconsultor.com"
}
```

**Validaciones:**
- `customerId` > 0
- `email` válido y único para el cliente
- `password` mínimo 8 caracteres, con al menos 1 mayúscula, 1 minúscula, 1 dígito
- `firstName` y `lastName` requeridos (máx 100 caracteres)

**Response 200 (Éxito):**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "Brand Partner user created successfully.",
  "data": {
    "brandPartnerUserId": 1,
    "customerId": 1,
    "email": "user@example.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "phoneNumber": "+506 8888-8888",
    "isActive": true,
    "emailVerified": false
  },
  "timestamp": "2026-02-28T10:30:00"
}
```

---

### 2. **POST** `/api/brandpartner/auth/login`
**Autenticación:** `[AllowAnonymous]`

**Descripción:** Paso 1 del login - valida email/password y envía código 2FA por email.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response 200 (Éxito):**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "2FA code sent to your email. Please check your inbox.",
  "data": {
    "requiresTwoFactor": true
  },
  "timestamp": "2026-02-28T10:30:00"
}
```

**Response 400 (Contraseña incorrecta):**
```json
{
  "status": false,
  "statusCode": 400,
  "message": "Invalid email or password. Remaining attempts: 4",
  "data": null,
  "timestamp": "2026-02-28T10:30:00"
}
```

**Response 400 (Cuenta bloqueada):**
```json
{
  "status": false,
  "statusCode": 400,
  "message": "Account is locked until 2026-02-28 11:00:00",
  "data": null,
  "timestamp": "2026-02-28T10:30:00"
}
```

---

### 3. **POST** `/api/brandpartner/auth/verify-2fa`
**Autenticación:** `[AllowAnonymous]`

**Descripción:** Paso 2 del login - verifica código 2FA y retorna JWT.

**Request Body:**
```json
{
  "email": "user@example.com",
  "code": "A1234"
}
```

**Validaciones:**
- `code` debe ser exactamente 5 caracteres: 1 letra mayúscula + 4 dígitos

**Response 200 (Éxito):**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "Login successful.",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "brandPartnerUserId": 1,
      "customerId": 1,
      "email": "user@example.com",
      "firstName": "Juan",
      "lastName": "Pérez",
      "phoneNumber": "+506 8888-8888",
      "isActive": true,
      "emailVerified": false,
      "requirePasswordChangeOnNextLogin": false
    },
    "requiresPasswordChange": false
  },
  "timestamp": "2026-02-28T10:30:00"
}
```

**Response 400 (Código inválido/expirado):**
```json
{
  "status": false,
  "statusCode": 400,
  "message": "Invalid or expired 2FA code.",
  "data": null,
  "timestamp": "2026-02-28T10:30:00"
}
```

---

### 4. **POST** `/api/brandpartner/auth/reset-password`
**Autenticación:** `[AllowAnonymous]`

**Descripción:** Restablece la contraseña enviando una contraseña temporal por email.

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response 200:**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "A temporary password has been sent to your email.",
  "data": null,
  "timestamp": "2026-02-28T10:30:00"
}
```

**Nota:** Por seguridad, el mensaje es el mismo si el email existe o no.

---

### 5. **POST** `/api/brandpartner/auth/change-password`
**Autenticación:** Requiere `[Authorize(Roles = "BrandPartner")]`

**Descripción:** Cambia la contraseña del usuario autenticado.

**Request Body:**
```json
{
  "oldPassword": "OldSecurePass123!",
  "newPassword": "NewSecurePass456!"
}
```

**Validaciones:**
- `newPassword` mínimo 8 caracteres, con al menos 1 mayúscula, 1 minúscula, 1 dígito
- `newPassword` debe ser diferente de `oldPassword`

**Response 200 (Éxito):**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "Password changed successfully.",
  "data": null,
  "timestamp": "2026-02-28T10:30:00"
}
```

**Response 400 (Contraseña actual incorrecta):**
```json
{
  "status": false,
  "statusCode": 400,
  "message": "Current password is incorrect.",
  "data": null,
  "timestamp": "2026-02-28T10:30:00"
}
```

---

### 6. **GET** `/api/brandpartner/auth/users`
**Autenticación:** Requiere `[Authorize(Roles = "Admin,Corporate")]`

**Descripción:** Lista usuarios Brand Partner por cliente.

**Query Parameters:**
- `customerId` (int, requerido) - ID del cliente
- `isActive` (bool, opcional) - Filtrar por activos/inactivos
- `emailVerified` (bool, opcional) - Filtrar por email verificado

**Ejemplo:** `GET /api/brandpartner/auth/users?customerId=1&isActive=true`

**Response 200:**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "Retrieved 3 users.",
  "data": [
    {
      "brandPartnerUserId": 1,
      "customerId": 1,
      "email": "user1@example.com",
      "firstName": "Juan",
      "lastName": "Pérez",
      "phoneNumber": "+506 8888-8888",
      "isActive": true,
      "emailVerified": false,
      "requirePasswordChangeOnNextLogin": false,
      "failedLoginAttempts": 0,
      "lockedUntil": null,
      "lastLogin": "2026-02-28T10:00:00",
      "createdAt": "2026-02-01T08:00:00"
    }
  ],
  "timestamp": "2026-02-28T10:30:00"
}
```

---

### 7. **GET** `/api/brandpartner/auth/login-history`
**Autenticación:** Requiere `[Authorize(Roles = "Admin,Corporate,BrandPartner")]`

**Descripción:** Obtiene el historial de login.

**Query Parameters:**
- `brandPartnerUserId` (int, opcional) - ID del usuario (obligatorio si el rol es BrandPartner)
- `dateFrom` (DateTime, opcional) - Fecha desde
- `dateTo` (DateTime, opcional) - Fecha hasta
- `ipAddress` (string, opcional) - Filtrar por IP
- `country` (string, opcional) - Filtrar por país
- `city` (string, opcional) - Filtrar por ciudad
- `loginSuccessful` (bool, opcional) - Filtrar por exitosos/fallidos
- `limit` (int, opcional, default: 100) - Máximo de registros

**Ejemplo:** `GET /api/brandpartner/auth/login-history?brandPartnerUserId=1&loginSuccessful=true&limit=50`

**Response 200:**
```json
{
  "status": true,
  "statusCode": 200,
  "message": "Retrieved 5 login history records.",
  "data": [
    {
      "loginHistoryId": 1,
      "brandPartnerUserId": 1,
      "loginDate": "2026-02-28T10:00:00",
      "ipAddress": "192.168.1.1",
      "userAgent": "Mozilla/5.0...",
      "location": "San José, Costa Rica",
      "country": "Costa Rica",
      "city": "San José",
      "loginSuccessful": true,
      "failureReason": null,
      "sessionId": "abc123..."
    }
  ],
  "timestamp": "2026-02-28T10:30:00"
}
```

**Nota:** Los usuarios con rol `BrandPartner` solo pueden ver su propio historial.

---

## Flujo de Autenticación 2FA

```
1. Usuario ingresa email/password en el frontend
   ↓
2. Frontend llama POST /api/brandpartner/auth/login
   ↓
3. Backend valida credenciales:
   - Si incorrectas: retorna error
   - Si correctas: genera código 2FA (ej: A1234) y lo envía por email
   ↓
4. Frontend muestra formulario para ingresar código 2FA
   ↓
5. Usuario recibe email y ingresa código
   ↓
6. Frontend llama POST /api/brandpartner/auth/verify-2fa con el código
   ↓
7. Backend valida código (no expirado, no usado):
   - Si válido: genera JWT y retorna token + datos de usuario
   - Si inválido: retorna error
   ↓
8. Frontend guarda token y redirige a la aplicación
   ↓
9. Si requiresPasswordChange = true, frontend fuerza cambio de contraseña
```

---

## Configuración de SendGrid

### Template IDs requeridos

En `appsettings.json` y `appsettings.Development.json`, configurar:

```json
{
  "SendGrid": {
    "ApiKey": "SG.xxx",
    "FromEmail": "noreply@vhconsultor.com",
    "FromName": "VH Consultor",
    "BrandPartnerTwoFactorCodeTemplateId": "d-xxxxxxxxxxxxxxxxxxxx",
    "BrandPartnerResetPasswordTemplateId": "d-xxxxxxxxxxxxxxxxxxxx",
    "SupportEmail": "support@vhconsultor.com"
  }
}
```

### Template 1: Código 2FA
**Variables dinámicas:**
- `{{ fullName }}` - Nombre completo del usuario
- `{{ code }}` - Código 2FA (ej: A1234)
- `{{ expiresIn }}` - Tiempo de expiración (siempre "3 minutos")

**Ejemplo de contenido:**
```
Hola {{ fullName }},

Tu código de verificación es: {{ code }}

Este código expira en {{ expiresIn }}.

Si no solicitaste este código, ignora este mensaje.

Saludos,
Equipo VH Consultor
```

### Template 2: Restablecer Contraseña
**Variables dinámicas:**
- `{{ fullName }}` - Nombre completo del usuario
- `{{ email }}` - Email del usuario
- `{{ newPassword }}` - Contraseña temporal generada

**Ejemplo de contenido:**
```
Hola {{ fullName }},

Tu contraseña ha sido restablecida. Tu nueva contraseña temporal es:

{{ newPassword }}

Por seguridad, deberás cambiar esta contraseña en tu próximo inicio de sesión.

Usuario: {{ email }}

Saludos,
Equipo VH Consultor
```

---

## Seguridad

### Contraseñas
- **Hash:** SHA256
- **Validación:** Mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 dígito

### Bloqueo de Cuenta
- **Máximo intentos fallidos:** 5
- **Tiempo de bloqueo:** 30 minutos
- **Reseteo:** Automático al login exitoso o después de 30 minutos

### Códigos 2FA
- **Formato:** 1 letra mayúscula (A-Z) + 4 dígitos (0-9)
- **Generación:** Aleatoria con `RandomNumberGenerator`
- **Expiración:** 3 minutos desde `CreatedAt`
- **Un solo uso:** Marcado como `IsUsed` después de verificación exitosa
- **Limpieza:** Códigos expirados se eliminan al generar uno nuevo

### JWT
- **Algoritmo:** HMAC-SHA256
- **Expiración:** 60 minutos (configurable en `appsettings.json`)
- **Claims:** UserId, Email, Role (BrandPartner)
- **Payload encriptado:** AES-256 con IV aleatorio

---

## Archivos Creados/Modificados

### SQL Scripts
- `Scripts/CreateBrandPartnerUsersTable.sql` - Script completo de creación de tablas

### Model Layer
- `ModelLayer/BrandPartner/Entities/BrandPartnerUser.cs`
- `ModelLayer/BrandPartner/Entities/BrandPartnerUserLoginHistory.cs`
- `ModelLayer/BrandPartner/Entities/BrandPartnerTwoFactorCode.cs`
- `ModelLayer/DBcontext.cs` - Añadidos DbSets y configuración OnModelCreating

### Business Layer

#### Commands
- `BusinessLayer/BrandPartner/Commands/CreateBrandPartnerUserCommand.cs`
- `BusinessLayer/BrandPartner/Commands/BrandPartnerLoginCommand.cs`
- `BusinessLayer/BrandPartner/Commands/VerifyTwoFactorCodeCommand.cs`
- `BusinessLayer/BrandPartner/Commands/GenerateTwoFactorCodeCommand.cs`
- `BusinessLayer/BrandPartner/Commands/ResetPasswordBrandPartnerCommand.cs`
- `BusinessLayer/BrandPartner/Commands/ChangePasswordBrandPartnerCommand.cs`
- `BusinessLayer/BrandPartner/Commands/BrandPartnerUserCommandRepository.cs`
- `BusinessLayer/BrandPartner/Commands/BrandPartnerTwoFactorCodeCommandRepository.cs`

#### Queries
- `BusinessLayer/BrandPartner/Queries/BrandPartnerUserQueryRepository.cs`
- `BusinessLayer/BrandPartner/Queries/BrandPartnerLoginHistoryQueryRepository.cs`
- `BusinessLayer/BrandPartner/Queries/BrandPartnerTwoFactorCodeQueryRepository.cs`

#### Validators
- `BusinessLayer/BrandPartner/Validators/CreateBrandPartnerUserValidator.cs`
- `BusinessLayer/BrandPartner/Validators/BrandPartnerLoginValidator.cs`
- `BusinessLayer/BrandPartner/Validators/VerifyTwoFactorCodeValidator.cs`
- `BusinessLayer/BrandPartner/Validators/ResetPasswordBrandPartnerValidator.cs`
- `BusinessLayer/BrandPartner/Validators/ChangePasswordBrandPartnerValidator.cs`

#### Settings
- `BusinessLayer/Shared/SendGridSettings.cs` - Añadidos 2 template IDs

### Application Layer
- `ApplicationLayer/BrandPartner/BrandPartnerAuthService.cs` - Servicio principal con toda la lógica 2FA

### API Layer
- `ApiLayer/Controllers/BrandPartner/BrandPartnerAuthController.cs` - Controller con 7 endpoints
- `ApiLayer/appsettings.json` - Añadidos template IDs de SendGrid
- `ApiLayer/appsettings.Development.json` - Añadidos template IDs de SendGrid
- `ApiLayer/Program.cs` - Registrados todos los servicios, repositories y validators en DI

---

## Compilación

El proyecto compila correctamente sin errores. Solo hay warnings pre-existentes no relacionados con Brand Partner.

**Estado:** ✅ Compilación exitosa (0 errores, 21 warnings pre-existentes)

---

## Próximos Pasos

1. **Ejecutar el script SQL** en la base de datos para crear las tablas `BrandPartner.*`
2. **Crear los templates en SendGrid** con las variables dinámicas especificadas
3. **Configurar los Template IDs** en `appsettings.json` y `appsettings.Development.json`
4. **Probar los endpoints** con Postman o herramienta similar
5. **Implementar la UI del frontend** para login 2FA y gestión de usuarios

---

## Notas Importantes

- Los usuarios Brand Partner deben estar vinculados a un `Customer` existente en `Corporate.Customers`
- El email debe ser único por cliente (un mismo email puede existir para diferentes clientes)
- Login siempre se realiza con email (no hay username)
- Los códigos 2FA expiran en 3 minutos y son de un solo uso
- Después de 5 intentos fallidos, la cuenta se bloquea por 30 minutos
- El flag `RequirePasswordChangeOnNextLogin` se activa automáticamente al resetear contraseña
- Los usuarios con rol `BrandPartner` solo pueden cambiar su propia contraseña y ver su propio historial
- Los roles `Admin` y `Corporate` pueden crear usuarios y ver todo el historial
