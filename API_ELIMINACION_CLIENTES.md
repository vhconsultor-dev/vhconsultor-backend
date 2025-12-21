# API: Eliminación de Clientes

## Endpoint

```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/{customerId}
```

## Descripción

Este endpoint permite eliminar (soft delete) un cliente del sistema. La eliminación es lógica, es decir, el registro no se elimina físicamente de la base de datos, sino que se marca como inactivo (`IsActive = false`).

**Importante:** El sistema valida que el cliente no tenga contratos asociados antes de permitir la eliminación. Si el cliente tiene contratos, se retorna un error con la lista de números de contratos asociados.

---

## Autenticación

Este endpoint requiere autenticación mediante JWT token.

**Header requerido:**
```
Authorization: Bearer {token}
```

---

## Parámetros

### Path Parameters

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `customerId` | `integer` | Sí | ID del cliente a eliminar |

### Ejemplo de URL

```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/123
```

---

## Request

Este endpoint **NO requiere body**. Todos los parámetros se envían en la URL.

### Headers

```
Content-Type: application/json
Authorization: Bearer {jwt_token}
```

---

## Response Structure

Todas las respuestas siguen la siguiente estructura estándar:

```json
{
  "status": boolean,
  "statusCode": integer,
  "data": any,
  "message": string,
  "errorNumber": string | null,
  "timestamp": string (ISO 8601)
}
```

### Campos de la Respuesta

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `status` | `boolean` | Indica si la operación fue exitosa (`true`) o falló (`false`) |
| `statusCode` | `integer` | Código de estado HTTP de la respuesta |
| `data` | `any` | Datos de la respuesta (puede ser `null` en caso de error) |
| `message` | `string` | Mensaje descriptivo del resultado de la operación |
| `errorNumber` | `string | null` | Número de error único (solo en caso de errores inesperados) |
| `timestamp` | `string` | Fecha y hora de la respuesta en formato ISO 8601 (zona horaria Costa Rica) |

---

## Códigos de Estado HTTP

| Código | Descripción | Cuándo se retorna |
|--------|-------------|-------------------|
| `200` | OK | Cliente eliminado exitosamente |
| `400` | Bad Request | El cliente tiene contratos asociados (validación de negocio) |
| `404` | Not Found | El cliente con el ID especificado no existe |
| `500` | Internal Server Error | Error inesperado del servidor |

---

## Respuestas

### ✅ 200 OK - Cliente Eliminado Exitosamente

**Response Body:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Cliente 'Nombre de la Empresa' eliminado exitosamente",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Descripción:** El cliente fue marcado como inactivo (`IsActive = false`) exitosamente. El mensaje incluye el nombre de la empresa del cliente eliminado.

---

### ❌ 400 Bad Request - Cliente con Contratos Asociados

**Response Body:**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "No se puede eliminar el cliente porque tiene 3 contratos asociados: CON-2025-001, CON-2025-002, CON-2025-003. Por favor, elimine o cancele estos contratos antes de eliminar el cliente.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Ejemplo con 1 contrato:**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "No se puede eliminar el cliente porque tiene 1 contrato asociado: CON-2025-001. Por favor, elimine o cancele estos contratos antes de eliminar el cliente.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Descripción:** El cliente tiene uno o más contratos asociados. El mensaje incluye:
- La cantidad de contratos (con singular/plural correcto)
- La lista completa de números de contratos separados por comas
- Instrucciones claras sobre qué hacer

**Acción requerida:** El usuario debe eliminar o cancelar todos los contratos asociados antes de poder eliminar el cliente.

---

### ❌ 404 Not Found - Cliente No Encontrado

**Response Body:**
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontró el cliente con ID 999",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Descripción:** El `customerId` proporcionado no existe en la base de datos.

---

### ❌ 500 Internal Server Error - Error Inesperado

**Response Body:**
```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error inesperado al eliminar el cliente: [descripción del error]",
  "errorNumber": "ERR-20251221-001234",
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

**Descripción:** Ocurrió un error inesperado en el servidor. El campo `errorNumber` contiene un identificador único que puede ser usado para reportar el error al soporte técnico.

---

## Validaciones

### Validaciones de Negocio

1. **Existencia del Cliente**
   - El cliente con el `customerId` especificado debe existir en la base de datos
   - Si no existe, retorna `404 Not Found`

2. **Contratos Asociados**
   - El cliente **NO** debe tener contratos asociados
   - Si tiene contratos, retorna `400 Bad Request` con la lista de números de contratos
   - La validación busca todos los contratos donde `CustomerId` coincida con el ID del cliente

### Validaciones de Entrada

1. **customerId**
   - Debe ser un número entero válido
   - Debe ser mayor a 0
   - Si no es válido, el framework retornará `400 Bad Request` automáticamente

---

## Comportamiento del Sistema

### Soft Delete

El sistema implementa **eliminación lógica (soft delete)**:

- El registro **NO se elimina físicamente** de la base de datos
- Se actualiza el campo `IsActive` a `false`
- Se actualiza el campo `UpdatedAt` con la fecha y hora actual (zona horaria Costa Rica)
- El cliente puede ser recuperado posteriormente si es necesario

### Validación de Contratos

Antes de eliminar el cliente, el sistema:

1. Consulta todos los contratos asociados al cliente
2. Si encuentra contratos:
   - Obtiene los números de contrato (`ContractNumber`)
   - Genera un mensaje descriptivo con la lista completa
   - Lanza una excepción `InvalidOperationException` que se convierte en `400 Bad Request`
3. Si no hay contratos:
   - Procede con la eliminación lógica

---

## Ejemplos de Uso

### Ejemplo 1: Eliminación Exitosa

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Cliente 'Acme Corporation' eliminado exitosamente",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Ejemplo 2: Cliente con Contratos

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/456
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "No se puede eliminar el cliente porque tiene 2 contratos asociados: CON-2025-001, CON-2025-015. Por favor, elimine o cancele estos contratos antes de eliminar el cliente.",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Ejemplo 3: Cliente No Encontrado

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/999
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (404 Not Found):**
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontró el cliente con ID 999",
  "errorNumber": null,
  "timestamp": "2025-12-21T10:30:45.1234567"
}
```

---

### Ejemplo 4: ID Inválido

**Request:**
```http
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/abc
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-...",
  "errors": {
    "customerId": [
      "The value 'abc' is not valid."
    ]
  }
}
```

---

## Flujo de Validación

```
┌─────────────────────────────────────┐
│  DELETE /Customer/{customerId}     │
└──────────────┬──────────────────────┘
               │
               ▼
    ┌──────────────────────┐
    │ ¿Cliente existe?     │
    └──────┬───────────────┘
           │
    ┌──────┴──────┐
    │             │
   NO            SÍ
    │             │
    ▼             ▼
┌────────┐  ┌──────────────────────┐
│  404   │  │ ¿Tiene contratos?   │
│  Not   │  └──────┬───────────────┘
│ Found  │         │
└────────┘    ┌────┴────┐
              │         │
             SÍ        NO
              │         │
              ▼         ▼
        ┌─────────┐  ┌──────────┐
        │   400   │  │   200    │
        │   Bad   │  │    OK    │
        │ Request │  │ Eliminado│
        └─────────┘  └──────────┘
```

---

## Notas Importantes

1. **Eliminación Lógica**: El cliente no se elimina físicamente, solo se marca como inactivo. Esto permite mantener el historial y la integridad referencial.

2. **Integridad de Datos**: La validación de contratos previene la eliminación de clientes con relaciones activas, manteniendo la integridad de los datos.

3. **Mensajes Amigables**: Los mensajes de error son descriptivos y proporcionan información específica (números de contratos) para ayudar al usuario a resolver el problema.

4. **Autenticación Requerida**: Este endpoint requiere un token JWT válido. Sin autenticación, retornará `401 Unauthorized`.

5. **Zona Horaria**: Todos los timestamps están en zona horaria de Costa Rica (UTC-6).

---

## Manejo de Errores en el Frontend

### Caso 1: Cliente con Contratos

```javascript
try {
  const response = await fetch(
    `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Customer/${customerId}`,
    {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );

  const data = await response.json();

  if (!data.status) {
    if (response.status === 400) {
      // Mostrar mensaje amigable con los números de contratos
      alert(data.message);
      // Opcional: extraer números de contratos del mensaje para mostrar en UI
    }
  }
} catch (error) {
  console.error('Error al eliminar cliente:', error);
}
```

### Caso 2: Extraer Números de Contratos del Mensaje

```javascript
// El mensaje tiene formato: "...tiene X contratos asociados: CON-2025-001, CON-2025-002..."
const message = data.message;
const contractsMatch = message.match(/:\s*([^.]+)/);
if (contractsMatch) {
  const contractNumbers = contractsMatch[1].split(', ').map(c => c.trim());
  console.log('Contratos asociados:', contractNumbers);
}
```

---

## Testing

### Casos de Prueba Recomendados

1. ✅ Eliminar cliente sin contratos → Debe retornar `200 OK`
2. ✅ Eliminar cliente con 1 contrato → Debe retornar `400 Bad Request` con mensaje singular
3. ✅ Eliminar cliente con múltiples contratos → Debe retornar `400 Bad Request` con lista completa
4. ✅ Eliminar cliente inexistente → Debe retornar `404 Not Found`
5. ✅ Eliminar con ID inválido (no numérico) → Debe retornar `400 Bad Request`
6. ✅ Eliminar sin autenticación → Debe retornar `401 Unauthorized`
7. ✅ Verificar que `IsActive` se actualiza a `false` después de eliminación exitosa
8. ✅ Verificar que `UpdatedAt` se actualiza con timestamp correcto

---

## Changelog

### Versión Actual
- ✅ Validación de contratos asociados
- ✅ Mensajes amigables con números de contratos
- ✅ Soft delete (eliminación lógica)
- ✅ Manejo de errores mejorado
- ✅ Respuestas estructuradas consistentes

---

## Soporte

Para reportar problemas o solicitar cambios, contactar al equipo de desarrollo con:
- El `errorNumber` (si está disponible)
- El `timestamp` de la respuesta
- Los detalles del request que causó el error

