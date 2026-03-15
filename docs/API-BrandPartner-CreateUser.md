# API de Creación de Usuario Brand Partner – Documentación detallada

Este documento describe con lujo de detalle el endpoint para **crear un usuario Brand Partner**. Este usuario pertenece a un cliente (Customer) existente en Corporate y podrá iniciar sesión en la plataforma Brand Partner con su correo y contraseña (flujo con 2FA).

---

## Información general del endpoint

**Método y URL:**  
`POST https://vh-apimanagement.azure-api.net/brandparthner-vh/api/brandpartner/auth/users`

**Autorización:**  
Requiere **JWT** en el header `Authorization: Bearer <token>`.  
Solo pueden llamar a este endpoint usuarios con rol **Admin** o **Corporate** (empleados/administradores de VH Consultor). Los usuarios con rol Brand Partner no tienen permiso.

**Propósito:**  
Crear un nuevo usuario Brand Partner vinculado a un cliente (Customer). Ese usuario podrá luego iniciar sesión con su email y contraseña en el flujo de autenticación Brand Partner (login + código 2FA). El email debe ser único **dentro de ese cliente**; el mismo email puede existir en otro cliente.

**Contexto de uso:**  
Por ejemplo: el cliente "Amig" tiene 10 empleados que necesitan acceso; el cliente "Cachito" tiene 15. Desde el sistema Corporate (con usuario Admin o Corporate) se llama a este API una vez por cada persona, indicando en cada llamada el `customerId` correspondiente.

---

## Formato de respuesta (envelope)

Todas las respuestas usan el mismo formato. El **código HTTP** suele ser **200**; el resultado real (éxito o error) se interpreta con el cuerpo JSON.

| Campo       | Tipo    | Descripción |
|------------|---------|-------------|
| `status`   | boolean | `true` = operación exitosa; `false` = error de validación o de negocio. |
| `statusCode` | number | Código lógico: 200 éxito, 400 validación/negocio, 500 error interno. |
| `message`  | string  | Mensaje descriptivo para el usuario o sistema que consume el API. |
| `data`     | object \| null | En éxito: objeto con los datos del usuario creado. En error: `null`. |
| `errorNumber` | string \| null | Opcional; identificador de error si aplica. |
| `timestamp` | string (ISO) | Fecha y hora de la respuesta. |

No confiar solo en el código HTTP; revisar siempre `status` y `statusCode` en el JSON.

---

## Parámetros del request (body JSON)

El contenido se envía en el **body** de la petición con **Content-Type: application/json**.

| Campo         | Tipo   | Obligatorio | Descripción y reglas |
|---------------|--------|-------------|----------------------|
| `customerId`  | number | Sí          | ID del cliente (Customer) al que pertenecerá el usuario. Debe ser un Customer existente en el esquema Corporate. Debe ser **mayor que 0**. |
| `email`       | string | Sí          | Correo del usuario. Será el identificador de login (siempre se usa email, no username). Debe ser un **formato de email válido** y tener **máximo 255 caracteres**. Debe ser **único para ese cliente**: no puede haber otro usuario Brand Partner con el mismo email en el mismo `customerId`. El sistema guarda el email en minúsculas. |
| `password`    | string | Sí          | Contraseña en texto plano. Se guarda hasheada (SHA256) en base de datos. **Mínimo 8 caracteres**. Debe contener **al menos una letra mayúscula**, **al menos una minúscula** y **al menos un dígito**. |
| `firstName`   | string | Sí          | Nombre de la persona. No puede estar vacío. **Máximo 100 caracteres**. |
| `lastName`    | string | Sí          | Apellido de la persona. No puede estar vacío. **Máximo 100 caracteres**. |
| `phoneNumber` | string | No          | Teléfono de contacto. Opcional. Si se envía, **máximo 50 caracteres**. |
| `createdBy`   | string | Sí (recomendado) | Identificador de quién crea el usuario (por ejemplo email del admin o "Sistema"). Se guarda en el campo de auditoría del usuario creado. |

**Ejemplo de body completo:**

```json
{
  "customerId": 5,
  "email": "maria.garcia@empresa.com",
  "password": "MiClaveSegura123",
  "firstName": "María",
  "lastName": "García López",
  "phoneNumber": "+506 8888-1234",
  "createdBy": "admin@vhconsultor.com"
}
```

**Ejemplo mínimo (sin teléfono):**

```json
{
  "customerId": 3,
  "email": "juan@cliente.com",
  "password": "PassWord1",
  "firstName": "Juan",
  "lastName": "Pérez",
  "createdBy": "admin@vhconsultor.com"
}
```

---

## Escenarios de respuesta

### 1. Creación exitosa

Cuando el `customerId` existe, el email es válido y no está ya usado para ese cliente, y el resto de validaciones pasan, el usuario se crea y se devuelve su información (sin la contraseña).

- **HTTP:** 200  
- **status:** `true`  
- **statusCode:** 200  
- **message:** `"Brand Partner user created successfully."`  
- **data:** Objeto con los datos del usuario creado (ver tabla abajo).  
- **errorNumber:** `null`

**Campos del objeto en `data`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `brandPartnerUserId` | number | ID único asignado al usuario en la tabla Brand Partner. |
| `customerId` | number | ID del cliente (igual al enviado). |
| `email` | string | Correo guardado (en minúsculas). |
| `firstName` | string | Nombre. |
| `lastName` | string | Apellido. |
| `phoneNumber` | string \| null | Teléfono si se envió; si no, `null`. |
| `isActive` | boolean | Siempre `true` al crear. |
| `emailVerified` | boolean | Siempre `false` al crear. |

**Ejemplo de respuesta:**

```json
{
  "status": true,
  "statusCode": 200,
  "message": "Brand Partner user created successfully.",
  "data": {
    "brandPartnerUserId": 42,
    "customerId": 5,
    "email": "maria.garcia@empresa.com",
    "firstName": "María",
    "lastName": "García López",
    "phoneNumber": "+506 8888-1234",
    "isActive": true,
    "emailVerified": false
  },
  "errorNumber": null,
  "timestamp": "2026-02-28T16:00:00.0000000"
}
```

A partir de aquí el usuario puede usar el endpoint de **login** Brand Partner con ese `email` y la contraseña que se envió al crearlo.

---

### 2. Customer no existe

El `customerId` no corresponde a un registro en la tabla de Customers (Corporate).

- **HTTP:** 200  
- **status:** `false`  
- **statusCode:** 400  
- **message:** `"Customer with ID {customerId} does not exist."` (con el ID enviado).  
- **data:** `null`

**Ejemplo:**

```json
{
  "status": false,
  "statusCode": 400,
  "message": "Customer with ID 999 does not exist.",
  "data": null,
  "errorNumber": null,
  "timestamp": "2026-02-28T16:00:00.0000000"
}
```

---

### 3. Email ya existe para ese cliente

Ya existe un usuario Brand Partner con el mismo email y el mismo `customerId`. El email es único por cliente, no a nivel global.

- **HTTP:** 200  
- **status:** `false`  
- **statusCode:** 400  
- **message:** `"A user with email '{email}' already exists for this customer."` (con el email enviado).  
- **data:** `null`

**Ejemplo:**

```json
{
  "status": false,
  "statusCode": 400,
  "message": "A user with email 'maria.garcia@empresa.com' already exists for this customer.",
  "data": null,
  "errorNumber": null,
  "timestamp": "2026-02-28T16:00:00.0000000"
}
```

---

### 4. Error de validación del body

Cuando algún campo no cumple las reglas (formato, longitud, obligatoriedad, etc.), la API responde con **statusCode 400** y en `message` uno o varios textos separados por `"; "`.

Posibles mensajes (solo o combinados):

| Mensaje | Cuándo aparece |
|---------|------------------|
| `"CustomerId must be greater than 0"` | `customerId` es 0 o negativo. |
| `"Email is required"` | `email` vacío o no enviado. |
| `"Invalid email format"` | `email` no tiene formato de correo válido. |
| `"Email must not exceed 255 characters"` | `email` tiene más de 255 caracteres. |
| `"Password is required"` | `password` vacío o no enviado. |
| `"Password must be at least 8 characters"` | `password` tiene menos de 8 caracteres. |
| `"Password must contain at least one uppercase letter"` | No hay ninguna mayúscula. |
| `"Password must contain at least one lowercase letter"` | No hay ninguna minúscula. |
| `"Password must contain at least one digit"` | No hay ningún número. |
| `"FirstName is required"` | `firstName` vacío o no enviado. |
| `"FirstName must not exceed 100 characters"` | `firstName` tiene más de 100 caracteres. |
| `"LastName is required"` | `lastName` vacío o no enviado. |
| `"LastName must not exceed 100 characters"` | `lastName` tiene más de 100 caracteres. |
| `"PhoneNumber must not exceed 50 characters"` | `phoneNumber` tiene más de 50 caracteres. |

**Ejemplo con varios errores:**

```json
{
  "status": false,
  "statusCode": 400,
  "message": "Email is required; Password must be at least 8 characters; Password must contain at least one uppercase letter",
  "data": null,
  "errorNumber": null,
  "timestamp": "2026-02-28T16:00:00.0000000"
}
```

---

### 5. No autorizado (falta o rol incorrecto)

Si no se envía el JWT o el token no tiene rol **Admin** o **Corporate**, la petición será rechazada por la capa de autorización.

- **HTTP:** Típicamente 401 (según configuración de API Management).  
- **status:** `false`  
- **statusCode:** 401 (en el cuerpo si la API lo devuelve).  
- **message:** Mensaje de no autorizado según la política (por ejemplo "Unauthorized").  
- **data:** `null`

El cliente debe enviar un token válido de un usuario Admin o Corporate.

---

### 6. Error interno del servidor

Cualquier excepción no controlada en el backend (base de datos, configuración, etc.) se traduce en:

- **HTTP:** 200  
- **status:** `false`  
- **statusCode:** 500  
- **message:** `"An error occurred: [detalle del error]."`  
- **data:** `null`

El detalle exacto depende del entorno; en producción suele ser un mensaje genérico por seguridad.

---

## Resumen de códigos lógicos

| statusCode | Significado | Ejemplo de message |
|------------|-------------|--------------------|
| 200 | Usuario creado correctamente | "Brand Partner user created successfully." |
| 400 | Validación o regla de negocio | "Customer with ID X does not exist.", "A user with email '...' already exists for this customer.", mensajes de validación de campos. |
| 401 | No autorizado | Token faltante o rol no Admin/Corporate. |
| 500 | Error interno | "An error occurred: ..." |

---

## Comportamiento técnico relevante (sin explicar cómo programar)

- **Email:** Se almacena en minúsculas. La unicidad se comprueba por pareja `(customerId, email)`.  
- **Contraseña:** Se hashea con SHA256 antes de guardar; nunca se devuelve en la respuesta.  
- **Valores por defecto al crear:** `IsActive = true`, `EmailVerified = false`, `RequirePasswordChangeOnNextLogin = false`, `FailedLoginAttempts = 0`.  
- **Auditoría:** Se guardan `CreatedAt` (fecha/hora de creación) y `CreatedBy` (valor enviado en el body).  
- **Relación:** El usuario queda vinculado al Customer por `customerId` (Foreign Key a Corporate.Customers).

---

## Uso del token (Authorization)

En el header de la petición:

```http
Authorization: Bearer <token>
```

El token debe corresponder a un usuario con rol **Admin** o **Corporate** (obtenido desde el flujo de autenticación Corporate, no desde el login Brand Partner).

---

## URL completa (API Management)

`POST https://vh-apimanagement.azure-api.net/brandparthner-vh/api/brandpartner/auth/users`

Este documento cubre únicamente el endpoint de **creación de usuario** Brand Partner. El resto de endpoints (login, verify-2fa, reset-password, change-password, listar usuarios, historial de login) se describen en **API-BrandPartner-Auth.md**.
