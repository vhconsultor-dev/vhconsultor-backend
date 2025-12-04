# Documentación API - Gestión de Contratos

## Base URL
```
https://vh-apimanagement.azure-api.net/corporate-vh
```

## Autenticación
Todos los endpoints requieren autenticación mediante JWT (JSON Web Token).

### Header requerido en todas las peticiones:
```
Authorization: Bearer {jwt_token}
```

### Respuesta 401 si el token no es válido o no se envía:
```json
{
  "status": false,
  "statusCode": 401,
  "data": null,
  "message": "No autorizado",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## Estructura de Respuesta Estándar

Todas las respuestas siguen esta estructura:

```json
{
  "status": boolean,
  "statusCode": number,
  "data": object | array | null,
  "message": string,
  "errorNumber": string | null,
  "timestamp": string (ISO 8601)
}
```

### Códigos de estado comunes:
- **200**: Operación exitosa
- **400**: Bad Request - Error en los datos enviados
- **401**: No autorizado - Token inválido o ausente
- **404**: No encontrado - Recurso no existe
- **422**: Error de validación - Datos inválidos según reglas de negocio
- **500**: Error interno del servidor

---

# 1. API de Contratos (Contract)

## 1.1. POST - Crear Contrato
**Endpoint:** `POST /api/corporate/Contract`

### Headers:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Body (JSON):
```json
{
  "contractId": "string",           // REQUERIDO - Máx 50 caracteres - ID único del contrato
  "customerId": number,             // REQUERIDO - Debe ser > 0
  "contractNumber": "string",       // REQUERIDO - Máx 100 caracteres
  "clientLegalName": "string",      // OPCIONAL - Máx 255 caracteres
  "clientTaxId": "string",          // OPCIONAL - Máx 50 caracteres
  "clientNationality": "string",    // OPCIONAL - Máx 100 caracteres
  "clientAddress": "string",        // OPCIONAL
  "clientPrimaryContact": "string", // OPCIONAL - Máx 255 caracteres
  "clientEmail": "string",          // OPCIONAL - Máx 255 caracteres - Debe ser email válido
  "clientPhone": "string",          // OPCIONAL - Máx 50 caracteres
  "contractTypeId": number,         // OPCIONAL
  "serviceDescription": "string",   // OPCIONAL
  "feeTypeId": number,              // OPCIONAL
  "feeAmount": number,              // OPCIONAL - Debe ser >= 0 - Formato decimal
  "feeDescription": "string",       // OPCIONAL
  "currencyCode": "string",         // OPCIONAL - Exactamente 3 caracteres (ej: "USD", "CRC")
  "contractTerm": "string",         // OPCIONAL - Máx 50 caracteres
  "paymentFrequency": "string",     // OPCIONAL - Máx 50 caracteres
  "paymentDay": number,             // OPCIONAL - Entre 1 y 31
  "paymentMethodId": number,        // OPCIONAL
  "signedDate": "string",           // OPCIONAL - Formato ISO 8601 (ej: "2024-12-02T00:00:00Z")
  "effectiveDate": "string",        // OPCIONAL - Formato ISO 8601 - Debe ser <= startDate
  "startDate": "string",            // OPCIONAL - Formato ISO 8601
  "endDate": "string",              // OPCIONAL - Formato ISO 8601 - Debe ser > startDate
  "autoRenewal": boolean,           // OPCIONAL
  "renewalTerm": "string",          // OPCIONAL - Máx 50 caracteres
  "renewalNoticeDays": number,      // OPCIONAL
  "noticePeriodDays": number,       // OPCIONAL
  "status": "string",               // OPCIONAL - Máx 50 caracteres (ej: "Activo", "Pendiente", "Cancelado")
  "governingLaw": "string",         // OPCIONAL - Máx 100 caracteres
  "disputeResolution": "string",    // OPCIONAL - Máx 100 caracteres
  "contractualDomicile": "string",  // OPCIONAL
  "jurisdiction": "string",         // OPCIONAL - Máx 100 caracteres
  "notes": "string",                // OPCIONAL
  "documentUrl": "string",          // OPCIONAL - Máx 500 caracteres - URL del documento
  "signedDocumentUrl": "string",    // OPCIONAL - Máx 500 caracteres - URL del documento firmado
  "lastModifiedBy": "string"        // OPCIONAL - Máx 255 caracteres
}
```

### Validaciones:
- `contractId`: Requerido, máximo 50 caracteres, debe ser único en el sistema
- `customerId`: Requerido, debe ser mayor a 0
- `contractNumber`: Requerido, máximo 100 caracteres
- `clientEmail`: Si se envía, debe ser un email válido
- `feeAmount`: Si se envía, debe ser >= 0
- `currencyCode`: Si se envía, debe tener exactamente 3 caracteres
- `paymentDay`: Si se envía, debe estar entre 1 y 31
- `endDate`: Si se envía junto con `startDate`, debe ser posterior a `startDate`
- `effectiveDate`: Si se envía junto con `startDate`, debe ser anterior o igual a `startDate`

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": "CON-2024-001",
  "message": "Contrato creado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Contrato Duplicado (400):
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Ya existe un contrato con el ID CON-2024-001",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Validación (422):
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "El ID del contrato es requerido, El ID del customer debe ser mayor a 0",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Servidor (500):
```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error al crear el contrato: [detalle del error]",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 1.2. GET - Obtener Contratos
**Endpoint:** `GET /api/corporate/Contract`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Query Parameters (Todos opcionales):
```
contractId=string           // ID específico del contrato
customerId=number           // Filtrar por ID del cliente
contractNumber=string       // Búsqueda parcial por número de contrato
status=string              // Filtrar por estado (ej: "Activo", "Cancelado")
contractTypeId=number      // Filtrar por tipo de contrato
currencyCode=string        // Filtrar por código de moneda (3 caracteres)
startDateFrom=string       // Fecha inicio desde (ISO 8601)
startDateTo=string         // Fecha inicio hasta (ISO 8601)
endDateFrom=string         // Fecha fin desde (ISO 8601)
endDateTo=string           // Fecha fin hasta (ISO 8601)
```

### Ejemplos de uso:

#### Obtener un contrato específico:
```
GET /api/corporate/Contract?contractId=CON-2024-001
```

#### Obtener todos los contratos de un cliente:
```
GET /api/corporate/Contract?customerId=123
```

#### Obtener contratos activos en USD:
```
GET /api/corporate/Contract?status=Activo&currencyCode=USD
```

#### Obtener todos los contratos (sin filtros):
```
GET /api/corporate/Contract
```

### Respuesta Exitosa - Un Contrato (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "contractId": "CON-2024-001",
    "customerId": 123,
    "contractNumber": "2024-001",
    "clientLegalName": "Empresa XYZ S.A.",
    "clientTaxId": "3-101-123456",
    "clientNationality": "Costa Rica",
    "clientAddress": "San José, Costa Rica",
    "clientPrimaryContact": "Juan Pérez",
    "clientEmail": "juan.perez@empresa.com",
    "clientPhone": "+506 2222-3333",
    "contractTypeId": 1,
    "serviceDescription": "Servicios de consultoría",
    "feeTypeId": 2,
    "feeAmount": 5000.00,
    "feeDescription": "Tarifa mensual",
    "currencyCode": "USD",
    "contractTerm": "12 meses",
    "paymentFrequency": "Mensual",
    "paymentDay": 15,
    "paymentMethodId": 1,
    "signedDate": "2024-01-15T00:00:00",
    "effectiveDate": "2024-02-01T00:00:00",
    "startDate": "2024-02-01T00:00:00",
    "endDate": "2025-02-01T00:00:00",
    "autoRenewal": true,
    "renewalTerm": "12 meses",
    "renewalNoticeDays": 30,
    "noticePeriodDays": 60,
    "status": "Activo",
    "governingLaw": "Leyes de Costa Rica",
    "disputeResolution": "Arbitraje",
    "contractualDomicile": "San José",
    "jurisdiction": "Costa Rica",
    "notes": "Contrato estándar",
    "createdAt": "2024-01-15T08:30:00",
    "updatedAt": "2024-01-16T10:00:00",
    "lastModifiedBy": "admin@empresa.com",
    "documentUrl": "https://storage.com/docs/CON-2024-001.pdf",
    "signedDocumentUrl": "https://storage.com/docs/CON-2024-001-signed.pdf"
  },
  "message": "Contrato obtenido exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Exitosa - Múltiples Contratos (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "contractId": "CON-2024-001",
      "customerId": 123,
      "contractNumber": "2024-001",
      // ... resto de campos
    },
    {
      "contractId": "CON-2024-002",
      "customerId": 123,
      "contractNumber": "2024-002",
      // ... resto de campos
    }
  ],
  "message": "Contratos obtenidos exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Contrato No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Contrato no encontrado",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 1.3. GET - Obtener Contratos Activos
**Endpoint:** `GET /api/corporate/Contract/active`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Query Parameters:
Ninguno

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "contractId": "CON-2024-001",
      "customerId": 123,
      "status": "Activo",
      // ... resto de campos del contrato
    }
  ],
  "message": "Contratos activos obtenidos exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

**Nota:** Este endpoint retorna contratos con estado "Activo" o "Vigente".

---

## 1.4. PUT - Actualizar Contrato
**Endpoint:** `PUT /api/corporate/Contract/{contractId}`

### Headers:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Path Parameters:
```
contractId (string) - ID del contrato a actualizar
```

### Body (JSON):
```json
{
  "customerId": number,             // REQUERIDO - Debe ser > 0
  "contractNumber": "string",       // REQUERIDO - Máx 100 caracteres
  "clientLegalName": "string",      // OPCIONAL - Máx 255 caracteres
  "clientTaxId": "string",          // OPCIONAL - Máx 50 caracteres
  "clientNationality": "string",    // OPCIONAL - Máx 100 caracteres
  "clientAddress": "string",        // OPCIONAL
  "clientPrimaryContact": "string", // OPCIONAL - Máx 255 caracteres
  "clientEmail": "string",          // OPCIONAL - Máx 255 caracteres - Debe ser email válido
  "clientPhone": "string",          // OPCIONAL - Máx 50 caracteres
  "contractTypeId": number,         // OPCIONAL
  "serviceDescription": "string",   // OPCIONAL
  "feeTypeId": number,              // OPCIONAL
  "feeAmount": number,              // OPCIONAL - Debe ser >= 0
  "feeDescription": "string",       // OPCIONAL
  "currencyCode": "string",         // OPCIONAL - Exactamente 3 caracteres
  "contractTerm": "string",         // OPCIONAL - Máx 50 caracteres
  "paymentFrequency": "string",     // OPCIONAL - Máx 50 caracteres
  "paymentDay": number,             // OPCIONAL - Entre 1 y 31
  "paymentMethodId": number,        // OPCIONAL
  "signedDate": "string",           // OPCIONAL - Formato ISO 8601
  "effectiveDate": "string",        // OPCIONAL - Formato ISO 8601 - Debe ser <= startDate
  "startDate": "string",            // OPCIONAL - Formato ISO 8601
  "endDate": "string",              // OPCIONAL - Formato ISO 8601 - Debe ser > startDate
  "autoRenewal": boolean,           // OPCIONAL
  "renewalTerm": "string",          // OPCIONAL - Máx 50 caracteres
  "renewalNoticeDays": number,      // OPCIONAL
  "noticePeriodDays": number,       // OPCIONAL
  "status": "string",               // OPCIONAL - Máx 50 caracteres
  "governingLaw": "string",         // OPCIONAL - Máx 100 caracteres
  "disputeResolution": "string",    // OPCIONAL - Máx 100 caracteres
  "contractualDomicile": "string",  // OPCIONAL
  "jurisdiction": "string",         // OPCIONAL - Máx 100 caracteres
  "notes": "string",                // OPCIONAL
  "documentUrl": "string",          // OPCIONAL - Máx 500 caracteres
  "signedDocumentUrl": "string",    // OPCIONAL - Máx 500 caracteres
  "lastModifiedBy": "string"        // OPCIONAL - Máx 255 caracteres
}
```

### Validaciones:
Las mismas que en el POST, excepto que no se envía `contractId` en el body (va en la URL).

### Ejemplo:
```
PUT /api/corporate/Contract/CON-2024-001
```

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Contrato actualizado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Contrato No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontró el contrato con ID CON-2024-001",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Validación (422):
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "El ID del customer debe ser mayor a 0, El número de contrato es requerido",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 1.5. DELETE - Cancelar Contrato
**Endpoint:** `DELETE /api/corporate/Contract/{contractId}`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Path Parameters:
```
contractId (string) - ID del contrato a cancelar
```

### Query Parameters (Opcional):
```
deletedBy=string  // Usuario que elimina el contrato
```

### Ejemplo:
```
DELETE /api/corporate/Contract/CON-2024-001?deletedBy=admin@empresa.com
```

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Contrato cancelado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Contrato No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontró el contrato con ID CON-2024-001",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

**Nota Importante:** Este endpoint NO elimina físicamente el contrato de la base de datos. Cambia el `status` del contrato a "Cancelado" (soft delete). El contrato sigue existiendo en el sistema para mantener el historial.

---

# 2. API de Servicios de Contrato (ContractService)

## 2.1. POST - Crear Servicio de Contrato
**Endpoint:** `POST /api/corporate/ContractService`

### Headers:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Body (JSON):
```json
{
  "contractId": "string",        // REQUERIDO - Máx 50 caracteres - Debe existir en la tabla Contracts
  "serviceId": number,           // OPCIONAL - Referencia a la tabla Services
  "serviceDescription": "string", // OPCIONAL - Descripción del servicio
  "regions": "string",           // OPCIONAL - Regiones donde aplica el servicio
  "unitPrice": number,           // OPCIONAL - Debe ser >= 0 - Formato decimal
  "quantity": number,            // OPCIONAL - Debe ser > 0 - Formato decimal
  "discountPercentage": number,  // OPCIONAL - Entre 0 y 100 - Formato decimal
  "finalPrice": number,          // OPCIONAL - Debe ser >= 0 - Formato decimal
  "serviceOrder": number,        // OPCIONAL - Debe ser > 0 - Orden de visualización
  "billingFrequency": "string"   // OPCIONAL - Máx 50 caracteres (ej: "Mensual", "Anual")
}
```

### Validaciones:
- `contractId`: Requerido, máximo 50 caracteres
- `unitPrice`: Si se envía, debe ser >= 0
- `quantity`: Si se envía, debe ser > 0
- `discountPercentage`: Si se envía, debe estar entre 0 y 100
- `finalPrice`: Si se envía, debe ser >= 0
- `serviceOrder`: Si se envía, debe ser > 0
- `billingFrequency`: Si se envía, máximo 50 caracteres

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": 1,
  "message": "Servicio del contrato creado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

**Nota:** El `data` contiene el `contractServiceId` (número entero) del servicio creado.

### Respuesta Error - Validación (422):
```json
{
  "status": false,
  "statusCode": 422,
  "data": null,
  "message": "El ID del contrato es requerido, La cantidad debe ser mayor a 0",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 2.2. GET - Obtener Servicios de Contrato
**Endpoint:** `GET /api/corporate/ContractService`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Query Parameters (Todos opcionales):
```
contractServiceId=number   // ID específico del servicio
contractId=string         // Filtrar por ID del contrato
serviceId=number          // Filtrar por ID del servicio
isActive=boolean          // Filtrar por estado activo (por defecto: true)
```

### Ejemplos de uso:

#### Obtener un servicio específico:
```
GET /api/corporate/ContractService?contractServiceId=1
```

#### Obtener todos los servicios de un contrato:
```
GET /api/corporate/ContractService?contractId=CON-2024-001
```

#### Obtener todos los servicios (activos e inactivos) de un contrato:
```
GET /api/corporate/ContractService?contractId=CON-2024-001&isActive=false
```

#### Obtener todos los servicios:
```
GET /api/corporate/ContractService
```

### Respuesta Exitosa - Un Servicio (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "contractServiceId": 1,
    "contractId": "CON-2024-001",
    "serviceId": 10,
    "serviceDescription": "Consultoría en gestión empresarial",
    "regions": "San José, Heredia",
    "unitPrice": 1000.00,
    "quantity": 5.00,
    "discountPercentage": 10.00,
    "finalPrice": 4500.00,
    "isActive": true,
    "serviceOrder": 1,
    "billingFrequency": "Mensual",
    "createdAt": "2024-01-15T08:30:00",
    "updatedAt": "2024-01-16T10:00:00"
  },
  "message": "Servicio del contrato obtenido exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Exitosa - Múltiples Servicios (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "contractServiceId": 1,
      "contractId": "CON-2024-001",
      "serviceDescription": "Consultoría",
      "finalPrice": 4500.00,
      "isActive": true,
      // ... resto de campos
    },
    {
      "contractServiceId": 2,
      "contractId": "CON-2024-001",
      "serviceDescription": "Soporte técnico",
      "finalPrice": 2000.00,
      "isActive": true,
      // ... resto de campos
    }
  ],
  "message": "Servicios del contrato obtenidos exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Servicio No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Servicio del contrato no encontrado",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 2.3. GET - Obtener Total del Contrato
**Endpoint:** `GET /api/corporate/ContractService/total/{contractId}`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Path Parameters:
```
contractId (string) - ID del contrato
```

### Ejemplo:
```
GET /api/corporate/ContractService/total/CON-2024-001
```

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": 6500.00,
  "message": "Total del contrato calculado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

**Nota:** El `data` contiene la suma de todos los `finalPrice` de los servicios activos (`isActive = true`) del contrato especificado.

### Respuesta Error (500):
```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error al calcular el total del contrato: [detalle del error]",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 2.4. PUT - Actualizar Servicio de Contrato
**Endpoint:** `PUT /api/corporate/ContractService/{contractServiceId}`

### Headers:
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

### Path Parameters:
```
contractServiceId (number) - ID del servicio de contrato a actualizar
```

### Body (JSON):
```json
{
  "contractId": "string",        // REQUERIDO - Máx 50 caracteres
  "serviceId": number,           // OPCIONAL
  "serviceDescription": "string", // OPCIONAL
  "regions": "string",           // OPCIONAL
  "unitPrice": number,           // OPCIONAL - Debe ser >= 0
  "quantity": number,            // OPCIONAL - Debe ser > 0
  "discountPercentage": number,  // OPCIONAL - Entre 0 y 100
  "finalPrice": number,          // OPCIONAL - Debe ser >= 0
  "serviceOrder": number,        // OPCIONAL - Debe ser > 0
  "billingFrequency": "string"   // OPCIONAL - Máx 50 caracteres
}
```

### Validaciones:
Las mismas que en el POST.

### Ejemplo:
```
PUT /api/corporate/ContractService/1
```

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Servicio del contrato actualizado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Servicio No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontró el servicio del contrato con ID 1",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

---

## 2.5. DELETE - Desactivar Servicio de Contrato
**Endpoint:** `DELETE /api/corporate/ContractService/{contractServiceId}`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Path Parameters:
```
contractServiceId (number) - ID del servicio de contrato a desactivar
```

### Ejemplo:
```
DELETE /api/corporate/ContractService/1
```

### Respuesta Exitosa (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": true,
  "message": "Servicio del contrato desactivado exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Servicio No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontró el servicio del contrato con ID 1",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

**Nota Importante:** Este endpoint NO elimina físicamente el servicio de la base de datos. Cambia `isActive` a `false` (soft delete). El servicio sigue existiendo en el sistema pero no se incluirá en los cálculos de totales ni en búsquedas que filtren por `isActive=true`.

---

# 3. API de Tipos de Contrato (ContractType)

## 3.1. GET - Obtener Tipos de Contrato
**Endpoint:** `GET /api/corporate/ContractType`

### Headers:
```
Authorization: Bearer {jwt_token}
```

### Query Parameters (Todos opcionales):
```
id=number          // ID específico del tipo de contrato
typeName=string    // Búsqueda parcial por nombre del tipo
isActive=boolean   // Filtrar por estado activo (por defecto: true)
```

### Ejemplos de uso:

#### Obtener un tipo específico:
```
GET /api/corporate/ContractType?id=1
```

#### Buscar por nombre:
```
GET /api/corporate/ContractType?typeName=Servicio
```

#### Obtener todos los tipos (activos e inactivos):
```
GET /api/corporate/ContractType?isActive=false
```

#### Obtener todos los tipos activos:
```
GET /api/corporate/ContractType
```

### Respuesta Exitosa - Un Tipo (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "contractTypeId": 1,
    "typeName": "Contrato de Servicios",
    "typeDescription": "Contrato para prestación de servicios profesionales",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00"
  },
  "message": "Contract type obtenido exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Exitosa - Múltiples Tipos (200):
```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "contractTypeId": 1,
      "typeName": "Contrato de Servicios",
      "typeDescription": "Contrato para prestación de servicios profesionales",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00"
    },
    {
      "contractTypeId": 2,
      "typeName": "Contrato de Compraventa",
      "typeDescription": "Contrato para compra de productos",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00"
    }
  ],
  "message": "Contract types obtenidos exitosamente",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

### Respuesta Error - Tipo No Encontrado (404):
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Contract type no encontrado",
  "errorNumber": null,
  "timestamp": "2024-12-02T10:30:00"
}
```

**Nota:** Esta API es de solo lectura (GET). No hay endpoints para crear, actualizar o eliminar tipos de contrato desde el frontend. Los tipos se gestionan a nivel de base de datos.

---

# 4. Flujo de Trabajo Recomendado

## 4.1. Creación de un Contrato Completo

### Paso 1: Obtener Tipos de Contrato (opcional)
```
GET /api/corporate/ContractType
```
Para mostrar un dropdown o lista de tipos disponibles.

### Paso 2: Crear el Contrato Principal
```
POST /api/corporate/Contract
```
Con todos los datos del contrato. Guardar el `contractId` retornado.

### Paso 3: Agregar Servicios al Contrato
```
POST /api/corporate/ContractService
```
Por cada servicio que forme parte del contrato, usando el `contractId` del paso anterior.

### Paso 4: Obtener Total del Contrato
```
GET /api/corporate/ContractService/total/{contractId}
```
Para mostrar el monto total del contrato.

---

## 4.2. Consulta de Contratos de un Cliente

### Paso 1: Obtener todos los contratos del cliente
```
GET /api/corporate/Contract?customerId=123
```

### Paso 2: Para cada contrato, obtener sus servicios
```
GET /api/corporate/ContractService?contractId=CON-2024-001
```

### Paso 3: Obtener total de cada contrato
```
GET /api/corporate/ContractService/total/CON-2024-001
```

---

## 4.3. Edición de un Contrato

### Paso 1: Obtener datos actuales del contrato
```
GET /api/corporate/Contract?contractId=CON-2024-001
```

### Paso 2: Actualizar el contrato
```
PUT /api/corporate/Contract/CON-2024-001
```
Con los datos modificados.

### Paso 3: Actualizar servicios si es necesario
```
PUT /api/corporate/ContractService/{contractServiceId}
```

---

## 4.4. Cancelación de un Contrato

### Opción 1: Cancelar solo el contrato (servicios quedan activos)
```
DELETE /api/corporate/Contract/CON-2024-001?deletedBy=usuario@empresa.com
```

### Opción 2: Cancelar contrato y desactivar todos sus servicios

#### Paso 1: Obtener todos los servicios del contrato
```
GET /api/corporate/ContractService?contractId=CON-2024-001
```

#### Paso 2: Desactivar cada servicio
```
DELETE /api/corporate/ContractService/1
DELETE /api/corporate/ContractService/2
...
```

#### Paso 3: Cancelar el contrato
```
DELETE /api/corporate/Contract/CON-2024-001?deletedBy=usuario@empresa.com
```

---

# 5. Consideraciones Importantes

## 5.1. Un Cliente Puede Tener Múltiples Contratos
- No hay restricción para que un `customerId` tenga múltiples contratos
- Cada contrato debe tener un `contractId` único
- Los contratos son independientes entre sí

## 5.2. Formato de Fechas
- Todas las fechas deben enviarse en formato ISO 8601: `YYYY-MM-DDTHH:mm:ss` o `YYYY-MM-DDTHH:mm:ssZ`
- Ejemplos válidos:
  - `2024-12-02T00:00:00`
  - `2024-12-02T10:30:00Z`
  - `2024-12-02T10:30:00-06:00`

## 5.3. Formato de Números Decimales
- Los campos decimales (`feeAmount`, `unitPrice`, `quantity`, `discountPercentage`, `finalPrice`) deben enviarse como números, no como strings
- Ejemplos:
  - ✅ Correcto: `"feeAmount": 5000.00`
  - ❌ Incorrecto: `"feeAmount": "5000.00"`

## 5.4. Campos Nullable vs Requeridos
- Los campos marcados como REQUERIDO deben enviarse siempre
- Los campos OPCIONAL pueden omitirse del JSON o enviarse como `null`
- Si un campo OPCIONAL no se envía, se guardará como `null` en la base de datos

## 5.5. Soft Delete
- Los DELETE no eliminan físicamente los registros
- Contract: Cambia el `status` a "Cancelado"
- ContractService: Cambia `isActive` a `false`
- Los registros eliminados se mantienen para historial y auditoría

## 5.6. Validaciones del Backend
- El backend valida TODOS los datos recibidos usando FluentValidation
- Si hay errores de validación, el backend retorna un código 422 con un mensaje detallado
- El frontend debe mostrar estos mensajes al usuario
- Los mensajes de validación están en español

## 5.7. Manejo de Errores
- Siempre validar el campo `status` de la respuesta
- Si `status: false`, mostrar el `message` al usuario
- Los códigos HTTP principales:
  - 200: Éxito
  - 400: Datos inválidos o error de negocio
  - 401: Token JWT inválido o ausente
  - 404: Recurso no encontrado
  - 422: Error de validación
  - 500: Error interno del servidor

## 5.8. Paginación
- Actualmente NO hay paginación implementada
- Los GET retornan todos los registros que cumplan los filtros
- Si se necesita paginación, debe solicitarse al backend

## 5.9. Ordenamiento
- Los contratos por cliente se ordenan por `CreatedAt DESC` (más recientes primero)
- Los tipos de contrato no tienen ordenamiento específico
- Los servicios de contrato pueden ordenarse usando el campo `serviceOrder`

## 5.10. Campos Calculados
- `finalPrice` en ContractService NO se calcula automáticamente
- El frontend debe calcular: `finalPrice = (unitPrice * quantity) - (unitPrice * quantity * discountPercentage / 100)`
- El total del contrato se obtiene mediante el endpoint `/total/{contractId}`

---

# 6. Ejemplos de Peticiones Completas

## 6.1. Crear un Contrato con Servicios

### Paso 1: Crear el contrato
```http
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "contractId": "CON-2024-001",
  "customerId": 123,
  "contractNumber": "2024-001",
  "clientLegalName": "Empresa XYZ S.A.",
  "clientEmail": "contacto@empresa.com",
  "contractTypeId": 1,
  "currencyCode": "USD",
  "status": "Activo",
  "startDate": "2024-01-01T00:00:00",
  "endDate": "2024-12-31T00:00:00"
}
```

### Paso 2: Agregar servicio 1
```http
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/ContractService
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "contractId": "CON-2024-001",
  "serviceDescription": "Consultoría estratégica",
  "unitPrice": 1000.00,
  "quantity": 5.00,
  "discountPercentage": 10.00,
  "finalPrice": 4500.00,
  "serviceOrder": 1,
  "billingFrequency": "Mensual"
}
```

### Paso 3: Agregar servicio 2
```http
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/ContractService
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "contractId": "CON-2024-001",
  "serviceDescription": "Soporte técnico",
  "unitPrice": 500.00,
  "quantity": 4.00,
  "discountPercentage": 0.00,
  "finalPrice": 2000.00,
  "serviceOrder": 2,
  "billingFrequency": "Mensual"
}
```

### Paso 4: Obtener total del contrato
```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/ContractService/total/CON-2024-001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Respuesta: `data: 6500.00`

---

## 6.2. Consultar Contratos de un Cliente

```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract?customerId=123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 6.3. Buscar Contratos Activos en USD

```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract?status=Activo&currencyCode=USD
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 6.4. Actualizar Estado de un Contrato

```http
PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/Contract/CON-2024-001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "customerId": 123,
  "contractNumber": "2024-001",
  "status": "Suspendido",
  "lastModifiedBy": "admin@empresa.com"
}
```

---

# 7. Códigos de Error Específicos

Si el backend retorna un `errorNumber`, puede indicar:

- **2627**: Violación de constraint único (ej: `contractId` duplicado)
- **547**: Violación de foreign key (ej: `customerId` no existe)
- **515**: Campo requerido faltante en base de datos

Estos son errores de SQL Server que el backend captura y convierte en mensajes legibles.

---

# 8. Testing de la API

## 8.1. Herramientas Recomendadas
- Postman
- Insomnia
- Thunder Client (VS Code)
- cURL

## 8.2. Variables de Entorno Sugeridas
```
BASE_URL=https://vh-apimanagement.azure-api.net/corporate-vh
JWT_TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 8.3. Colección de Postman
Se recomienda crear una colección con:
- Todas las peticiones documentadas
- Variables para `BASE_URL` y `JWT_TOKEN`
- Tests para validar respuestas exitosas
- Ejemplos de errores comunes

---

# 9. Soporte y Contacto

Para dudas, problemas o solicitudes de nuevos endpoints, contactar al equipo de backend.

**Última actualización:** 2024-12-02

