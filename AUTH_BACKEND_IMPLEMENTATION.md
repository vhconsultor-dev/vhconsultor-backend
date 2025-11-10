# Documentación Backend - Módulo de Autenticación

## 📋 Índice
1. [Flujo de Login](#flujo-de-login)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [Condiciones y Validaciones](#condiciones-y-validaciones)
4. [Manejo de Errores](#manejo-de-errores)
5. [Estados de Cuenta](#estados-de-cuenta)

---

## 🔐 Flujo de Login

### Endpoint Principal
**POST** `/api/Auth/login`

### Orden de Llamadas

1. **Primera llamada: Login**
   - Endpoint: `POST /api/Auth/login`
   - Autenticación: No requerida (`[AllowAnonymous]`)
   - Body requerido:
     ```json
     {
       "usernameOrEmail": "string",
       "password": "string"
     }
     ```

### Información Automática Capturada por el Backend

El backend captura automáticamente:
- `IPAddress`: Desde `HttpContext.Connection.RemoteIpAddress`
- `UserAgent`: Desde el header `User-Agent` de la solicitud
- `LoginDate`: Timestamp del servidor

**Opcional** (pueden enviarse en el body para mayor precisión):
- `location`: Ubicación geográfica completa
- `country`: País
- `city`: Ciudad
- `region`: Región/Estado
- `deviceType`: Tipo de dispositivo
- `browser`: Navegador
- `operatingSystem`: Sistema operativo

---

## 📡 Endpoints Disponibles

### 1. POST /api/Auth/login

**Autenticación:** No requerida

**Request Body:**
```json
{
  "usernameOrEmail": "usuario@ejemplo.com" o "username",
  "password": "contraseña123",
  "location": "San José, Costa Rica" (opcional),
  "country": "Costa Rica" (opcional),
  "city": "San José" (opcional),
  "region": "San José" (opcional),
  "deviceType": "Desktop" (opcional),
  "browser": "Chrome" (opcional),
  "operatingSystem": "Windows" (opcional)
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "user": {
      "userId": 1,
      "firstName": "Juan",
      "lastName": "Pérez",
      "email": "usuario@ejemplo.com",
      "username": "jperez",
      "phoneNumber": "+50612345678",
      "profilePictureUrl": null,
      "isCorporate": true,
      "isBrandPartner": false,
      "isActive": true,
      "emailVerified": true,
      "lastLogin": "2024-12-20T10:30:00",
      "lastLoginIP": "192.168.1.1",
      "lastLoginLocation": "San José, Costa Rica",
      "lastLoginCountry": "Costa Rica",
      "lastLoginCity": "San José",
      "createdAt": "2024-01-15T08:00:00"
    },
    "message": "Inicio de sesión exitoso"
  },
  "message": "Inicio de sesión exitoso"
}
```

**Nota Importante:** El campo `passwordHash` nunca se devuelve en las respuestas (se oculta por seguridad).

---

### 2. POST /api/Auth/change-password

**Autenticación:** Requerida (`[Authorize]`)

**Request Body:**
```json
{
  "userId": 1,
  "currentPassword": "contraseñaActual123",
  "newPassword": "nuevaContraseña456",
  "confirmNewPassword": "nuevaContraseña456"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "message": "Contraseña cambiada exitosamente"
  },
  "message": "Contraseña cambiada exitosamente"
}
```

---

### 3. POST /api/Auth/lock-account

**Autenticación:** Requerida (`[Authorize]`)

**Request Body:**
```json
{
  "userId": 1,
  "lockDurationMinutes": 30,
  "reason": "Violación de políticas" (opcional),
  "lockedBy": "admin@ejemplo.com" (opcional)
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "message": "Cuenta bloqueada hasta 2024-12-20T11:00:00",
    "lockedUntil": "2024-12-20T11:00:00"
  },
  "message": "Cuenta bloqueada hasta 2024-12-20T11:00:00"
}
```

---

### 4. POST /api/Auth/unlock-account

**Autenticación:** Requerida (`[Authorize]`)

**Request Body:**
```json
{
  "userId": 1,
  "unlockedBy": "admin@ejemplo.com" (opcional)
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "message": "Cuenta desbloqueada exitosamente"
  },
  "message": "Cuenta desbloqueada exitosamente"
}
```

---

### 5. GET /api/Auth/users

**Autenticación:** Requerida (`[Authorize]`)

**Query Parameters (todos opcionales):**
- `userId`: int - Si se especifica, devuelve solo ese usuario
- `email`: string - Búsqueda parcial por email
- `username`: string - Búsqueda parcial por username
- `isActive`: bool - Filtrar por estado activo
- `isCorporate`: bool - Filtrar por tipo corporativo
- `isBrandPartner`: bool - Filtrar por tipo Brand Partner
- `emailVerified`: bool - Filtrar por email verificado

**Ejemplo de llamada:**
```
GET /api/Auth/users?userId=1
GET /api/Auth/users?email=usuario&isActive=true
GET /api/Auth/users?isCorporate=true&emailVerified=true
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "userId": 1,
      "firstName": "Juan",
      "lastName": "Pérez",
      "email": "usuario@ejemplo.com",
      "username": "jperez",
      "isActive": true,
      "isCorporate": true,
      "emailVerified": true,
      "failedLoginAttempts": 0,
      "lockedUntil": null,
      "lastLogin": "2024-12-20T10:30:00"
    }
  ],
  "message": "1 usuario(s) encontrado(s)"
}
```

**Respuesta cuando no se encuentra (404 Not Found):**
```json
{
  "success": false,
  "data": null,
  "message": "No se encontraron usuarios con los criterios especificados"
}
```

---

### 6. GET /api/Auth/login-history

**Autenticación:** Requerida (`[Authorize]`)

**Query Parameters (todos opcionales):**
- `userId`: int - Filtrar por ID de usuario
- `loginDateFrom`: DateTime - Fecha de inicio del rango
- `loginDateTo`: DateTime - Fecha de fin del rango
- `ipAddress`: string - Filtrar por dirección IP
- `country`: string - Búsqueda parcial por país
- `city`: string - Búsqueda parcial por ciudad
- `loginSuccessful`: bool - Filtrar por login exitoso (true) o fallido (false)
- `limit`: int - Límite de registros (por defecto 100)

**Ejemplo de llamada:**
```
GET /api/Auth/login-history?userId=1&limit=50
GET /api/Auth/login-history?loginDateFrom=2024-12-01&loginDateTo=2024-12-31
GET /api/Auth/login-history?loginSuccessful=false&limit=20
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "loginHistoryId": 1,
      "userId": 1,
      "loginDate": "2024-12-20T10:30:00",
      "ipAddress": "192.168.1.1",
      "location": "San José, Costa Rica",
      "country": "Costa Rica",
      "city": "San José",
      "region": "San José",
      "userAgent": "Mozilla/5.0...",
      "deviceType": "Desktop",
      "browser": "Chrome",
      "operatingSystem": "Windows",
      "loginSuccessful": true,
      "failureReason": null,
      "sessionId": null
    }
  ],
  "message": "1 registro(s) de historial encontrado(s)"
}
```

---

## ✅ Condiciones y Validaciones

### Validaciones del Backend para Login

#### 1. Validación de Campos Requeridos
El backend valida que:
- `usernameOrEmail` no esté vacío y tenga máximo 255 caracteres
- `password` no esté vacío

**Error de Validación (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "El nombre de usuario o email es requerido, La contraseña es requerida"
}
```

#### 2. Verificación de Usuario Existente
El backend verifica si el usuario existe buscando por `username` o `email`.

**Error si no existe (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Usuario o contraseña incorrectos"
}
```

**Condición a verificar:** El mensaje es genérico por seguridad (no revela si el usuario existe o no).

#### 3. Verificación de Cuenta Activa
El backend verifica que `IsActive = true`.

**Error si cuenta inactiva (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "La cuenta está inactiva. Contacte al administrador."
}
```

**Condición a verificar:** Si se recibe este mensaje, la cuenta requiere activación por parte de un administrador.

#### 4. Verificación de Cuenta Bloqueada
El backend verifica que `LockedUntil` sea null o que la fecha haya expirado.

**Error si cuenta bloqueada (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "La cuenta está bloqueada hasta 2024-12-20T11:00:00"
}
```

**Condición a verificar:** 
- Si se recibe este mensaje, la cuenta está bloqueada temporalmente
- El mensaje incluye la fecha/hora hasta la cual está bloqueada
- El bloqueo puede ser automático (por intentos fallidos) o manual (por administrador)

#### 5. Verificación de Contraseña
El backend compara el hash SHA256 de la contraseña enviada con el hash almacenado.

**Error si contraseña incorrecta (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Usuario o contraseña incorrectos. Intentos restantes: 3"
}
```

**Condición a verificar:**
- El mensaje incluye los intentos restantes antes del bloqueo automático
- Después de 5 intentos fallidos, la cuenta se bloquea automáticamente por 30 minutos
- El contador de intentos fallidos se resetea cuando el login es exitoso

#### 6. Bloqueo Automático por Intentos Fallidos
El backend bloquea automáticamente la cuenta después de 5 intentos fallidos.

**Error después de bloqueo automático (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Demasiados intentos fallidos. La cuenta ha sido bloqueada por 30 minutos."
}
```

**Condición a verificar:**
- El bloqueo es automático y dura 30 minutos
- No se puede desbloquear manualmente hasta que expire el tiempo
- El contador de intentos fallidos se incrementa con cada intento fallido

---

### Validaciones del Backend para Cambio de Contraseña

#### 1. Validación de Campos Requeridos
- `userId` debe ser mayor a 0
- `currentPassword` no debe estar vacío
- `newPassword` no debe estar vacío
- `confirmNewPassword` no debe estar vacío

#### 2. Validación de Fortaleza de Contraseña
El backend valida que la nueva contraseña cumpla con:
- Mínimo 8 caracteres
- Al menos una letra mayúscula
- Al menos una letra minúscula
- Al menos un número
- Al menos un carácter especial

**Error de validación (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "La nueva contraseña debe tener al menos 8 caracteres, La nueva contraseña debe contener al menos una letra mayúscula, ..."
}
```

#### 3. Validación de Coincidencia de Contraseñas
El backend valida que `newPassword` y `confirmNewPassword` sean iguales.

**Error si no coinciden (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Las contraseñas no coinciden"
}
```

#### 4. Validación de Contraseña Actual
El backend verifica que la contraseña actual sea correcta.

**Error si contraseña actual incorrecta (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "La contraseña actual es incorrecta"
}
```

#### 5. Validación de Diferencia de Contraseñas
El backend valida que la nueva contraseña sea diferente a la actual.

**Error si son iguales (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "La nueva contraseña debe ser diferente a la contraseña actual"
}
```

---

### Validaciones del Backend para Bloqueo/Desbloqueo de Cuenta

#### Bloqueo Manual
- `userId` debe ser mayor a 0
- `lockDurationMinutes` debe ser mayor a 0 y máximo 10080 (7 días)
- `reason` es opcional, máximo 500 caracteres

**Error de validación (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "La duración del bloqueo debe ser mayor a 0 minutos, La duración del bloqueo no puede exceder 7 días (10080 minutos)"
}
```

#### Desbloqueo
- `userId` debe ser mayor a 0

**Error si usuario no existe (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Usuario no encontrado"
}
```

---

## ⚠️ Manejo de Errores

### Códigos de Estado HTTP

#### 200 OK
- Login exitoso
- Cambio de contraseña exitoso
- Bloqueo/Desbloqueo exitoso
- Consulta de usuarios/historial exitosa

#### 400 Bad Request
- Errores de validación (FluentValidation)
- Usuario no encontrado
- Contraseña incorrecta
- Cuenta inactiva
- Cuenta bloqueada
- Intentos fallidos excedidos

#### 401 Unauthorized
- Token JWT inválido o expirado (para endpoints con `[Authorize]`)
- Token no proporcionado

#### 404 Not Found
- No se encontraron usuarios con los criterios especificados
- No se encontró historial de login con los criterios especificados

#### 500 Internal Server Error
- Errores inesperados del servidor

### Estructura de Respuesta de Error

```json
{
  "success": false,
  "data": null,
  "message": "Mensaje descriptivo del error"
}
```

---

## 🔒 Estados de Cuenta

### Estados que el Backend Maneja

#### 1. Cuenta Activa (`IsActive = true`)
- El usuario puede hacer login normalmente
- Si `IsActive = false`, el login falla con mensaje: "La cuenta está inactiva. Contacte al administrador."

#### 2. Cuenta Bloqueada (`LockedUntil` no null y fecha futura)
- El login falla con mensaje que incluye la fecha/hora de desbloqueo
- El bloqueo puede ser:
  - **Automático**: Por 5 intentos fallidos, dura 30 minutos
  - **Manual**: Por administrador, duración configurable (máximo 7 días)

#### 3. Intentos Fallidos (`FailedLoginAttempts`)
- Se incrementa con cada login fallido
- Se resetea a 0 cuando el login es exitoso
- Cuando llega a 5, la cuenta se bloquea automáticamente

#### 4. Email Verificado (`EmailVerified`)
- Campo informativo, no bloquea el login
- Puede usarse para mostrar advertencias o restricciones en el frontend

---

## 📊 Registro de Historial de Login

### Información que el Backend Registra Automáticamente

Cada intento de login (exitoso o fallido) se registra en `UserLoginHistory` con:

- `LoginDate`: Fecha y hora del intento
- `IPAddress`: Dirección IP (capturada automáticamente)
- `UserAgent`: Navegador y sistema operativo (capturado automáticamente)
- `LoginSuccessful`: true/false
- `FailureReason`: Razón del fallo (si aplica):
  - "Usuario no encontrado"
  - "Contraseña incorrecta"
  - "Cuenta inactiva"
  - "Cuenta bloqueada"

**Información opcional** que puede enviarse en el body del login:
- `location`, `country`, `city`, `region`
- `deviceType`, `browser`, `operatingSystem`

---

## 🔐 Seguridad

### Medidas de Seguridad Implementadas en el Backend

1. **Hash de Contraseñas**: SHA256 (no se almacenan en texto plano)
2. **Ocultación de Información Sensible**: `passwordHash` nunca se devuelve en respuestas
3. **Mensajes Genéricos**: Los errores de login no revelan si el usuario existe o no
4. **Bloqueo Automático**: Protección contra ataques de fuerza bruta
5. **Registro de Intentos**: Historial completo de todos los intentos de login
6. **Autenticación JWT**: Endpoints protegidos requieren token válido

---

## 📝 Notas Importantes

1. **Timezone**: Las fechas se manejan en UTC-6 (Costa Rica) usando `DateTimeService.GetCostaRicaNow()`

2. **Límite de Historial**: Por defecto, el historial de login devuelve máximo 100 registros. Se puede ajustar con el parámetro `limit`.

3. **Búsquedas Parciales**: Los parámetros `email`, `username`, `country`, `city` usan búsqueda parcial (LIKE '%valor%').

4. **Orden de Resultados**: 
   - Usuarios: Ordenados por `CreatedAt DESC` (más recientes primero)
   - Historial: Ordenado por `LoginDate DESC` (más recientes primero)

5. **Validación de JWT**: Los endpoints con `[Authorize]` requieren un token JWT válido en el header:
   ```
   Authorization: Bearer {token}
   ```

---

**Última actualización**: Diciembre 2024  
**Versión del Backend**: .NET 8.0

