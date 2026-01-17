# Especificación de APIs de Tokens para Frontend

## 📋 Resumen

Este documento especifica las dos APIs de generación de tokens que deben invocarse **inmediatamente después de un login exitoso**:

1. **API de JWT (Token propio)**: Genera el token JWT para autenticación en la aplicación
2. **API de Amazon**: Genera el access token de Amazon SP-API para integraciones con Amazon

---

## 🔐 API 1: Generación de Token JWT

### Endpoint
```
POST https://vh-apimanagement.azure-api.net/shared-vh/api/Security/generate-token
```

### Método HTTP
`POST`

### Headers
```
Content-Type: application/json
Accept: application/json
```

### 🔑 Keys Requeridas

El frontend necesita tener configuradas las siguientes keys para generar el payload encriptado:

| Key | Descripción | Uso |
|-----|-------------|-----|
| **SecretKey** | Clave secreta para validación | Se incluye en el payload antes de encriptar |
| **ValidationKey** | Clave para encriptar el payload | Se usa para encriptar el payload con AES-256 |

**⚠️ IMPORTANTE**: Estas keys deben ser proporcionadas por el equipo de backend y configuradas de forma segura en el frontend (variables de entorno, archivos de configuración seguros, etc.).

### Request Body
```json
{
  "encryptedPayload": "string_encriptado_requerido"
}
```

#### Descripción del Request
- **encryptedPayload** (string, requerido): Payload encriptado que debe generarse antes de hacer la petición

#### Proceso para Generar el encryptedPayload

1. **Crear el payload sin encriptar:**
   ```
   payload = "{SecretKey}|{timestamp}"
   ```
   - `SecretKey`: La clave secreta proporcionada por el backend
   - `timestamp`: Timestamp en milisegundos Unix (UTC-6, hora de Costa Rica)
   - Ejemplo: `"VHC0n5ult0r!2024$S3cur3K3y#F0rJWT@T0k3nG3n3r4t10n&V4l1d4t10n|1737144000000"`

2. **Encriptar el payload usando AES-256-CBC:**
   - Algoritmo: AES-256
   - Modo: CBC
   - Padding: PKCS7
   - Key: Derivar de ValidationKey usando SHA256 (32 bytes)
   - IV: Generar aleatoriamente (16 bytes)
   - Formato: Base64 del resultado (IV + datos encriptados)

3. **El encryptedPayload resultante** es lo que se envía en el request body

#### Ejemplo de Generación del Payload

```javascript
// Pseudocódigo
const secretKey = "VHC0n5ult0r!2024$S3cur3K3y#F0rJWT@T0k3nG3n3r4t10n&V4l1d4t10n";
const validationKey = "AES256V4l1d4t10nK3y!2024@VHC0n5ult0r#Encryp7&D3cryp7$S3cur3";

// 1. Generar timestamp en UTC-6 (Costa Rica)
const timestamp = Date.now() - (6 * 60 * 60 * 1000); // UTC-6

// 2. Crear payload
const payload = `${secretKey}|${timestamp}`;

// 3. Encriptar con AES-256-CBC usando validationKey
const encryptedPayload = encryptAES256(payload, validationKey);

// 4. Enviar al endpoint
```

#### Validaciones del Payload
- El timestamp debe estar dentro de **±1 minuto** de diferencia con la hora actual del servidor (UTC-6)
- El SecretKey debe coincidir exactamente con el configurado en el servidor
- El payload debe estar correctamente encriptado con AES-256 usando el ValidationKey

### Response Exitoso (200 OK)
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-01-17T17:30:00Z",
    "message": "Token generado exitosamente"
  },
  "message": "Token generado exitosamente",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

#### Campos del Response
- **status** (boolean): `true` si la operación fue exitosa
- **statusCode** (number): Código HTTP (200 para éxito)
- **data.success** (boolean): Indica si el token se generó correctamente
- **data.token** (string): Token JWT generado (usar para autenticación en requests posteriores)
- **data.expiresAt** (string, ISO 8601): Fecha y hora de expiración del token
- **data.message** (string): Mensaje descriptivo
- **message** (string): Mensaje general de la respuesta
- **timestamp** (string, ISO 8601): Timestamp de la respuesta

### Response de Error (400 Bad Request)
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "El payload encriptado es inválido",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

### Response de Error - Timestamp Expirado (400 Bad Request)
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "El timestamp ha expirado. Diferencia: 2.5 minutos",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

### Validaciones y Condicionales

#### ✅ Validaciones del Request
1. **encryptedPayload** debe estar presente y no estar vacío
2. El payload desencriptado debe tener el formato: `{SecretKey}|{timestamp}`
3. El timestamp debe estar dentro de **±1 minuto** de diferencia con la hora actual del servidor (UTC-6)
4. El **SecretKey** debe coincidir exactamente con el configurado en el servidor
5. El payload debe estar correctamente encriptado con **AES-256-CBC** usando el **ValidationKey**

#### ⚠️ Consideraciones Importantes
- **Timing**: El timestamp debe generarse justo antes de hacer la petición (no más de 1 minuto de diferencia)
- **Encriptación**: Usar AES-256-CBC con PKCS7 padding
- **Key Derivation**: La ValidationKey se deriva usando SHA256 para obtener 32 bytes
- **IV**: Se genera aleatoriamente y se incluye al inicio del payload encriptado

#### ⚠️ Manejo de Errores
- Si `status === false` y `statusCode === 400`: Error de validación o payload inválido
- Si `status === false` y `statusCode === 500`: Error interno del servidor
- Si el timestamp expiró: Generar un nuevo payload con timestamp actual y reintentar

#### 💾 Almacenamiento
- Guardar `data.token` para usar en el header `Authorization: Bearer {token}` en requests posteriores
- Guardar `data.expiresAt` para verificar expiración antes de hacer requests
- Si el token está próximo a expirar (menos de 5 minutos), generar uno nuevo

---

## 🛒 API 2: Generación de Token de Amazon

### Endpoint
```
POST https://vh-apimanagement.azure-api.net/shared-vh/api/amazon/AmazonAuth/generate-token
```

### Método HTTP
`POST`

### Headers
```
Content-Type: application/json
Accept: application/json
```

### Request Body
```json
{}
```

**Nota**: Este endpoint NO requiere body. Las credenciales se obtienen automáticamente de las variables de entorno configuradas en el servidor.

### Response Exitoso (200 OK)
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "accessToken": "Atza|IwEBIJ7JdF7_dG_yKsRv6t4eRv...",
    "tokenType": "bearer",
    "expiresIn": 3600,
    "expiresAt": "2026-01-17T17:30:00Z"
  },
  "message": "Access token generado exitosamente",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

#### Campos del Response
- **status** (boolean): `true` si la operación fue exitosa
- **statusCode** (number): Código HTTP (200 para éxito)
- **data.accessToken** (string): Access token de Amazon SP-API (usar para llamadas a Amazon)
- **data.tokenType** (string): Tipo de token, siempre `"bearer"`
- **data.expiresIn** (number): Tiempo de expiración en segundos (3600 = 1 hora)
- **data.expiresAt** (string, ISO 8601): Fecha y hora de expiración del token
- **message** (string): Mensaje descriptivo
- **timestamp** (string, ISO 8601): Timestamp de la respuesta

### Response de Error - Variables no Configuradas (400 Bad Request)
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "La variable de entorno Amazon__RefreshToken no está configurada",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

### Response de Error - Refresh Token Inválido (400 Bad Request)
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Error de Amazon: El refresh token no es válido. Puede haber sido revocado o expirado. Verifica que el refresh token en las variables de entorno de Azure sea correcto.",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

### Response de Error - Sin Access Token (400 Bad Request)
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "La respuesta de Amazon no contiene un access token válido. Respuesta recibida: {...}",
  "errorNumber": null,
  "timestamp": "2026-01-17T16:30:00"
}
```

### Validaciones y Condicionales

#### ✅ Validaciones del Request
1. No requiere parámetros en el body
2. El endpoint obtiene las credenciales automáticamente del servidor

#### ⚠️ Manejo de Errores
- Si `status === false` y `statusCode === 400`: 
  - Variables de entorno no configuradas
  - Refresh token inválido o revocado
  - Amazon no devolvió un token válido
- Si `status === false` y `statusCode === 500`: Error interno del servidor

#### 💾 Almacenamiento
- Guardar `data.accessToken` para usar en llamadas a Amazon SP-API
- Guardar `data.expiresAt` para verificar expiración
- El token expira en 1 hora (3600 segundos)
- Si el token está próximo a expirar (menos de 10 minutos), generar uno nuevo

---

## 🔄 Flujo de Inicio de Sesión

### Secuencia de Llamadas

1. **Login del Usuario**
   ```
   POST /api/Auth/login
   ```
   - Usuario ingresa credenciales
   - Si login es exitoso, continuar con los siguientes pasos

2. **Generar Token JWT** (Inmediatamente después del login exitoso)
   ```
   POST https://vh-apimanagement.azure-api.net/shared-vh/api/Security/generate-token
   ```
   - Generar el payload encriptado
   - Hacer la petición
   - Guardar el token JWT

3. **Generar Token de Amazon** (Inmediatamente después del login exitoso, puede ser en paralelo con el JWT)
   ```
   POST https://vh-apimanagement.azure-api.net/shared-vh/api/amazon/AmazonAuth/generate-token
   ```
   - Hacer la petición sin body
   - Guardar el access token de Amazon

### Diagrama de Flujo

```
Usuario → Login → ✅ Login Exitoso
                    ↓
        ┌───────────┴───────────┐
        ↓                       ↓
   Generar JWT          Generar Amazon Token
        ↓                       ↓
   Guardar JWT          Guardar Amazon Token
        └───────────┬───────────┘
                    ↓
            Continuar con la App
```

---

## 📝 Notas Importantes

### Token JWT
- **Duración**: Configurada en el servidor (típicamente 60 minutos)
- **Uso**: Header `Authorization: Bearer {token}` en todas las peticiones autenticadas
- **Renovación**: Generar nuevo token antes de que expire

### Token de Amazon
- **Duración**: 1 hora (3600 segundos)
- **Uso**: Header `Authorization: Bearer {accessToken}` o `x-amz-access-token: {accessToken}` en llamadas a Amazon SP-API
- **Renovación**: Puede generarse múltiples veces, no hay límite

### Manejo de Errores Común
- Si alguna de las dos APIs falla, mostrar mensaje de error al usuario
- Si el JWT falla: El usuario no podrá autenticarse en la aplicación
- Si el Amazon token falla: Las funcionalidades de Amazon no estarán disponibles, pero la app puede continuar

### Orden de Ejecución
- Ambas APIs pueden ejecutarse en **paralelo** (recomendado para mejor performance)
- Si se ejecutan secuencialmente, el orden no importa
- **Importante**: Ambas deben ejecutarse inmediatamente después del login exitoso

---

## 🔍 Códigos de Estado HTTP

| Código | Significado | Acción |
|--------|-------------|--------|
| 200 | Éxito | Continuar con el flujo normal |
| 400 | Bad Request | Revisar el mensaje de error, puede ser validación o credenciales inválidas |
| 401 | Unauthorized | Token inválido o expirado |
| 422 | Validation Error | Error de validación en los datos enviados |
| 500 | Internal Server Error | Error del servidor, reintentar más tarde |

---

## ✅ Checklist de Implementación

### Configuración Inicial
- [ ] Obtener **SecretKey** del equipo de backend
- [ ] Obtener **ValidationKey** del equipo de backend
- [ ] Configurar las keys de forma segura (variables de entorno, archivos de configuración)
- [ ] Implementar función de encriptación AES-256-CBC

### Implementación de APIs
- [ ] Implementar generación de payload encriptado para JWT
- [ ] Invocar API de JWT después de login exitoso
- [ ] Invocar API de Amazon después de login exitoso
- [ ] Guardar token JWT para autenticación
- [ ] Guardar access token de Amazon para llamadas a Amazon

### Manejo de Errores y Validaciones
- [ ] Implementar manejo de errores para ambas APIs
- [ ] Validar que el timestamp esté dentro del rango permitido (±1 minuto)
- [ ] Manejar errores de encriptación/desencriptación
- [ ] Verificar expiración de tokens antes de usarlos
- [ ] Implementar renovación automática de tokens cuando estén próximos a expirar
- [ ] Manejar casos donde una API falle pero la otra tenga éxito

### Seguridad
- [ ] Nunca exponer SecretKey o ValidationKey en el código fuente
- [ ] Usar variables de entorno o servicios de gestión de secretos
- [ ] No loggear las keys en consola o archivos de log

---

## 📞 Soporte

Para dudas sobre la implementación o problemas con las APIs, contactar al equipo de backend.
