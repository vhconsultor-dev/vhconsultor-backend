# 📋 Documentación para Frontend - Sistema de Facturas

## Índice
1. [Resumen General](#resumen-general)
2. [Endpoints Disponibles](#endpoints-disponibles)
3. [Estructuras JSON](#estructuras-json)
4. [Flujos de Trabajo](#flujos-de-trabajo)
5. [Casos de Uso](#casos-de-uso)
6. [Validaciones y Errores](#validaciones-y-errores)

---

## Resumen General

El sistema de facturas permite generar automáticamente todas las facturas de un contrato basándose en:
- **Monto del contrato** (`FeeAmount` o suma de `ContractServices`)
- **Frecuencia de pago** (`PaymentFrequency`: Monthly, Quarterly, SemiAnnual, Annual)
- **Período del contrato** (`StartDate` y `EndDate`)

### Estados de Factura
| Estado | Descripción | Cuándo se usa |
|--------|-------------|---------------|
| `Draft` | Borrador | Estado inicial al generar |
| `Sent` | Enviada | Cuando se envía al cliente |
| `Paid` | Pagada | Cuando se marca como pagada |
| `Overdue` | Vencida | Cuando pasa la fecha de vencimiento sin pagar |
| `Cancelled` | Cancelada | Cuando se cancela la factura |

### Estados de Pago
| Estado | Descripción |
|--------|-------------|
| `Unpaid` | No pagada (por defecto) |
| `Paid` | Pagada completamente |

---

## Endpoints Disponibles

### 1. Generar Facturas Automáticamente

**Descripción:** Genera todas las facturas de un contrato en un solo paso. Evalúa si ya existen facturas y previene duplicados.

**Endpoint:**
```
POST /api/corporate/invoice/generate/{contractId}
```

**Headers Requeridos:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Parámetros de URL:**
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `contractId` | string | Sí | ID del contrato (ej: "CONT-001") |

**Request Body:**
```
No requiere body
```

**Response Exitoso (200 OK):**
```json
{
  "success": true,
  "message": "Se generaron 12 facturas exitosamente",
  "data": {
    "success": true,
    "message": "Se generaron 12 facturas exitosamente",
    "invoicesGenerated": 12,
    "existingInvoicesCount": 0,
    "totalAmount": 12000.00,
    "invoiceIds": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]
  }
}
```

**Response si ya existen facturas (400 Bad Request):**
```json
{
  "success": false,
  "message": "Ya existen facturas generadas para este contrato",
  "data": {
    "success": false,
    "message": "Ya existen facturas generadas para este contrato",
    "invoicesGenerated": 0,
    "existingInvoicesCount": 12,
    "totalAmount": 0.00,
    "invoiceIds": []
  }
}
```

**Response si el contrato no existe (404 Not Found):**
```json
{
  "success": false,
  "message": "Contrato con ID CONT-999 no encontrado",
  "data": null
}
```

**Response si faltan datos en el contrato (400 Bad Request):**
```json
{
  "success": false,
  "message": "Validación fallida: El contrato debe tener StartDate, El contrato debe tener EndDate, El contrato debe tener PaymentFrequency",
  "data": null
}
```

**Comportamiento:**
1. ✅ Valida que el contrato exista
2. ✅ Verifica si ya hay facturas generadas (previene duplicados)
3. ✅ Valida que el contrato tenga todos los campos requeridos:
   - `StartDate`
   - `EndDate`
   - `PaymentFrequency`
   - `CurrencyCode`
   - `FeeAmount` O `ContractServices` activos
4. ✅ Calcula el número de facturas según la frecuencia
5. ✅ Genera todas las facturas en estado `Draft`
6. ✅ Asigna números de factura únicos (INV-2024-001, INV-2024-002, etc.)
7. ✅ Calcula fechas de emisión y vencimiento
8. ✅ Crea los items de cada factura

**Cuándo usar:**
- Cuando el usuario presiona el botón "Generar Facturas" en la pantalla de contrato
- Después de crear un nuevo contrato
- Cuando se activa un contrato

---

### 2. Obtener Facturas con Filtros (Query Unificado)

**Descripción:** Obtiene una lista de facturas con múltiples opciones de filtrado, ordenamiento y paginación. Este es el endpoint principal para consultar facturas.

**Endpoint:**
```
GET /api/corporate/invoice
```

**Headers Requeridos:**
```
Authorization: Bearer {token}
```

**Parámetros de Query (todos opcionales):**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `invoiceId` | int | ID específico de factura | `?invoiceId=1` |
| `contractId` | string | Filtrar por contrato | `?contractId=CONT-001` |
| `invoiceNumber` | string | Buscar por número (búsqueda parcial) | `?invoiceNumber=INV-2024` |
| `status` | string | Filtrar por estado (Draft, Sent, Paid, Overdue, Cancelled) | `?status=Draft` |
| `paymentStatus` | string | Filtrar por estado de pago (Unpaid, Paid) | `?paymentStatus=Unpaid` |
| `currencyCode` | string | Filtrar por moneda | `?currencyCode=USD` |
| `invoiceDateFrom` | DateTime | Fecha de emisión desde (formato: YYYY-MM-DD) | `?invoiceDateFrom=2024-01-01` |
| `invoiceDateTo` | DateTime | Fecha de emisión hasta (formato: YYYY-MM-DD) | `?invoiceDateTo=2024-12-31` |
| `dueDateFrom` | DateTime | Fecha de vencimiento desde | `?dueDateFrom=2024-01-01` |
| `dueDateTo` | DateTime | Fecha de vencimiento hasta | `?dueDateTo=2024-12-31` |
| `isOverdue` | bool | Solo facturas vencidas | `?isOverdue=true` |
| `orderBy` | string | Ordenamiento (InvoiceDate, DueDate, Total, Status) | `?orderBy=DueDate` |
| `pageNumber` | int | Número de página (inicia en 1) | `?pageNumber=1` |
| `pageSize` | int | Cantidad de registros por página | `?pageSize=10` |

**Ejemplos de URLs completas:**

```
# Todas las facturas de un contrato
GET /api/corporate/invoice?contractId=CONT-001

# Facturas en borrador
GET /api/corporate/invoice?status=Draft

# Facturas vencidas
GET /api/corporate/invoice?isOverdue=true

# Facturas no pagadas de un contrato con paginación
GET /api/corporate/invoice?contractId=CONT-001&paymentStatus=Unpaid&pageNumber=1&pageSize=10

# Facturas de enero 2024 ordenadas por fecha de vencimiento
GET /api/corporate/invoice?invoiceDateFrom=2024-01-01&invoiceDateTo=2024-01-31&orderBy=DueDate

# Buscar por número de factura
GET /api/corporate/invoice?invoiceNumber=INV-2024-001
```

**Response Exitoso (200 OK):**
```json
{
  "success": true,
  "message": "Se encontraron 12 factura(s)",
  "data": {
    "invoices": [
      {
        "invoiceId": 1,
        "contractId": "CONT-001",
        "invoiceNumber": "INV-2024-001",
        "invoiceDate": "2024-01-15T00:00:00",
        "dueDate": "2024-02-15T00:00:00",
        "subTotal": 1000.00,
        "tax": 0.00,
        "total": 1000.00,
        "currencyCode": "USD",
        "status": "Draft",
        "paymentStatus": "Unpaid",
        "paidDate": null,
        "paidBy": null,
        "paymentMethodId": null,
        "paymentReference": null,
        "depositNumber": null,
        "transferNumber": null,
        "notes": null,
        "createdAt": "2024-01-15T10:00:00",
        "updatedAt": null,
        "lastModifiedBy": null
      },
      {
        "invoiceId": 2,
        "contractId": "CONT-001",
        "invoiceNumber": "INV-2024-002",
        "invoiceDate": "2024-02-15T00:00:00",
        "dueDate": "2024-03-15T00:00:00",
        "subTotal": 1000.00,
        "tax": 0.00,
        "total": 1000.00,
        "currencyCode": "USD",
        "status": "Paid",
        "paymentStatus": "Paid",
        "paidDate": "2024-02-10T00:00:00",
        "paidBy": 5,
        "paymentMethodId": 1,
        "paymentReference": "REF-12345",
        "depositNumber": "DEP-67890",
        "transferNumber": null,
        "notes": "Pago recibido",
        "createdAt": "2024-01-15T10:00:00",
        "updatedAt": "2024-02-10T14:30:00",
        "lastModifiedBy": "5"
      }
    ],
    "totalRecords": 12,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 2
  }
}
```

**Response sin resultados (200 OK):**
```json
{
  "success": true,
  "message": "Se encontraron 0 factura(s)",
  "data": {
    "invoices": [],
    "totalRecords": 0,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 0
  }
}
```

**Comportamiento:**
1. ✅ Si no se envían filtros, devuelve todas las facturas
2. ✅ Los filtros se pueden combinar (ej: contractId + status + paymentStatus)
3. ✅ La búsqueda por `invoiceNumber` es parcial (busca coincidencias)
4. ✅ Si se usa paginación, devuelve `totalRecords` y `totalPages`
5. ✅ El ordenamiento por defecto es por fecha de creación descendente

**Cuándo usar:**
- Para mostrar la lista de facturas de un contrato
- Para buscar facturas específicas
- Para mostrar facturas vencidas en un dashboard
- Para filtrar facturas por estado o fecha

---

### 3. Obtener Detalle de Factura por ID

**Descripción:** Obtiene el detalle completo de una factura específica, incluyendo sus items (líneas) y adjuntos.

**Endpoint:**
```
GET /api/corporate/invoice/{invoiceId}
```

**Headers Requeridos:**
```
Authorization: Bearer {token}
```

**Parámetros de URL:**
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `invoiceId` | int | Sí | ID de la factura (ej: 1) |

**Ejemplo:**
```
GET /api/corporate/invoice/1
```

**Response Exitoso (200 OK):**
```json
{
  "success": true,
  "message": "Factura obtenida exitosamente",
  "data": {
    "invoice": {
      "invoiceId": 1,
      "contractId": "CONT-001",
      "invoiceNumber": "INV-2024-001",
      "invoiceDate": "2024-01-15T00:00:00",
      "dueDate": "2024-02-15T00:00:00",
      "subTotal": 1000.00,
      "tax": 0.00,
      "total": 1000.00,
      "currencyCode": "USD",
      "status": "Draft",
      "paymentStatus": "Unpaid",
      "paidDate": null,
      "paidBy": null,
      "paymentMethodId": null,
      "paymentReference": null,
      "depositNumber": null,
      "transferNumber": null,
      "notes": null,
      "createdAt": "2024-01-15T10:00:00",
      "updatedAt": null,
      "lastModifiedBy": null
    },
    "items": [
      {
        "invoiceItemId": 1,
        "invoiceId": 1,
        "contractServiceId": null,
        "description": "Servicios del contrato",
        "quantity": 1.00,
        "unitPrice": 1000.00,
        "discount": 0.00,
        "lineTotal": 1000.00,
        "serviceOrder": null,
        "createdAt": "2024-01-15T10:00:00"
      }
    ],
    "attachments": []
  }
}
```

**Ejemplo con múltiples items (cuando el contrato tiene servicios):**
```json
{
  "success": true,
  "message": "Factura obtenida exitosamente",
  "data": {
    "invoice": {
      "invoiceId": 5,
      "contractId": "CONT-002",
      "invoiceNumber": "INV-2024-005",
      "invoiceDate": "2024-01-01T00:00:00",
      "dueDate": "2024-02-01T00:00:00",
      "subTotal": 2250.00,
      "tax": 0.00,
      "total": 2250.00,
      "currencyCode": "USD",
      "status": "Paid",
      "paymentStatus": "Paid",
      "paidDate": "2024-01-25T00:00:00",
      "paidBy": 3,
      "paymentMethodId": 2,
      "paymentReference": "TRANSFER-98765",
      "depositNumber": null,
      "transferNumber": "TRF-54321",
      "notes": "Pago por transferencia bancaria",
      "createdAt": "2024-01-01T08:00:00",
      "updatedAt": "2024-01-25T16:45:00",
      "lastModifiedBy": "3"
    },
    "items": [
      {
        "invoiceItemId": 10,
        "invoiceId": 5,
        "contractServiceId": 15,
        "description": "Gestión de Redes Sociales - Facebook, Instagram",
        "quantity": 1.00,
        "unitPrice": 1500.00,
        "discount": 0.00,
        "lineTotal": 1500.00,
        "serviceOrder": 1,
        "createdAt": "2024-01-01T08:00:00"
      },
      {
        "invoiceItemId": 11,
        "invoiceId": 5,
        "contractServiceId": 16,
        "description": "Pauta Publicitaria Digital",
        "quantity": 1.00,
        "unitPrice": 750.00,
        "discount": 0.00,
        "lineTotal": 750.00,
        "serviceOrder": 2,
        "createdAt": "2024-01-01T08:00:00"
      }
    ],
    "attachments": [
      {
        "invoiceAttachmentId": 1,
        "invoiceId": 5,
        "fileUrl": "https://storage.example.com/invoices/comprobante-001.pdf",
        "fileName": "comprobante-pago-enero.pdf",
        "fileType": "application/pdf",
        "fileSize": 245678,
        "uploadedBy": 3,
        "uploadedAt": "2024-01-25T16:50:00"
      }
    ]
  }
}
```

**Response si no existe (404 Not Found):**
```json
{
  "success": false,
  "message": "Factura con ID 999 no encontrada",
  "data": null
}
```

**Comportamiento:**
1. ✅ Devuelve la factura completa con todos sus campos
2. ✅ Incluye todos los items (líneas) de la factura ordenados por `serviceOrder`
3. ✅ Incluye todos los adjuntos (comprobantes de pago) ordenados por fecha de carga
4. ✅ Si no hay items o adjuntos, devuelve arrays vacíos

**Cuándo usar:**
- Para mostrar el detalle completo de una factura
- Para imprimir o generar PDF de factura
- Para ver los servicios incluidos en la factura
- Para ver los comprobantes de pago adjuntos

---

### 4. Obtener Resumen de Facturas por Contrato

**Descripción:** Obtiene un resumen estadístico de todas las facturas de un contrato (totales, pagadas, pendientes, vencidas).

**Endpoint:**
```
GET /api/corporate/invoice/summary/{contractId}
```

**Headers Requeridos:**
```
Authorization: Bearer {token}
```

**Parámetros de URL:**
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `contractId` | string | Sí | ID del contrato (ej: "CONT-001") |

**Ejemplo:**
```
GET /api/corporate/invoice/summary/CONT-001
```

**Response Exitoso (200 OK):**
```json
{
  "success": true,
  "message": "Resumen de facturas obtenido exitosamente",
  "data": {
    "totalInvoices": 12,
    "paidInvoices": 5,
    "unpaidInvoices": 7,
    "overdueInvoices": 2,
    "totalAmount": 12000.00,
    "paidAmount": 5000.00,
    "unpaidAmount": 7000.00
  }
}
```

**Descripción de campos:**
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `totalInvoices` | int | Total de facturas generadas |
| `paidInvoices` | int | Cantidad de facturas pagadas |
| `unpaidInvoices` | int | Cantidad de facturas sin pagar |
| `overdueInvoices` | int | Cantidad de facturas vencidas |
| `totalAmount` | decimal | Monto total de todas las facturas |
| `paidAmount` | decimal | Monto total pagado |
| `unpaidAmount` | decimal | Monto total pendiente de pago |

**Response si el contrato no tiene facturas:**
```json
{
  "success": true,
  "message": "Resumen de facturas obtenido exitosamente",
  "data": {
    "totalInvoices": 0,
    "paidInvoices": 0,
    "unpaidInvoices": 0,
    "overdueInvoices": 0,
    "totalAmount": 0.00,
    "paidAmount": 0.00,
    "unpaidAmount": 0.00
  }
}
```

**Comportamiento:**
1. ✅ Calcula estadísticas en tiempo real
2. ✅ Incluye todas las facturas del contrato sin importar su estado
3. ✅ Los montos están en la moneda del contrato

**Cuándo usar:**
- Para mostrar un resumen en la pantalla de detalle del contrato
- Para dashboards o reportes
- Para mostrar el progreso de pagos de un contrato
- Para alertas de facturas vencidas

---

### 5. Marcar Factura como Pagada

**Descripción:** Marca una factura como pagada y registra los detalles del pago (fecha, método, referencias, etc.).

**Endpoint:**
```
PUT /api/corporate/invoice/{invoiceId}/mark-as-paid
```

**Headers Requeridos:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Parámetros de URL:**
| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `invoiceId` | int | Sí | ID de la factura a marcar como pagada |

**Request Body:**
```json
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 1,
  "paymentReference": "REF-12345",
  "depositNumber": "DEP-67890",
  "transferNumber": "TRF-11111"
}
```

**Campos del Request:**
| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `paidDate` | DateTime | No | Fecha del pago (formato: YYYY-MM-DD). Si no se envía, usa la fecha actual |
| `paymentMethodId` | int | No | ID del método de pago (1=Efectivo, 2=Transferencia, 3=Tarjeta, etc.) |
| `paymentReference` | string | No | Referencia del pago (máx 255 caracteres) |
| `depositNumber` | string | No | Número de depósito bancario (máx 255 caracteres) |
| `transferNumber` | string | No | Número de transferencia bancaria (máx 255 caracteres) |

**Ejemplos de Request Body:**

**Ejemplo 1: Pago mínimo (solo fecha):**
```json
{
  "paidDate": "2024-02-15"
}
```

**Ejemplo 2: Pago con transferencia bancaria:**
```json
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 2,
  "paymentReference": "TRANSFER-2024-001",
  "transferNumber": "TRF-98765432"
}
```

**Ejemplo 3: Pago con depósito:**
```json
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 1,
  "paymentReference": "DEPOSIT-2024-001",
  "depositNumber": "DEP-12345678"
}
```

**Ejemplo 4: Pago sin fecha (usa fecha actual):**
```json
{
  "paymentMethodId": 2,
  "paymentReference": "PAYMENT-TODAY"
}
```

**Response Exitoso (200 OK):**
```json
{
  "success": true,
  "message": "Factura marcada como pagada exitosamente",
  "data": {
    "success": true,
    "message": "Factura marcada como pagada exitosamente",
    "invoiceId": 1,
    "paidDate": "2024-02-15T00:00:00"
  }
}
```

**Response si la factura ya está pagada (400 Bad Request):**
```json
{
  "success": false,
  "message": "La factura ya está pagada",
  "data": null
}
```

**Response si la factura está cancelada (400 Bad Request):**
```json
{
  "success": false,
  "message": "No se puede pagar una factura cancelada",
  "data": null
}
```

**Response si la factura no existe (404 Not Found):**
```json
{
  "success": false,
  "message": "Factura con ID 999 no encontrada",
  "data": null
}
```

**Response si la fecha es futura (400 Bad Request):**
```json
{
  "success": false,
  "message": "La fecha de pago no puede ser futura",
  "data": null
}
```

**Comportamiento:**
1. ✅ Cambia el `status` de la factura a `Paid`
2. ✅ Cambia el `paymentStatus` a `Paid`
3. ✅ Registra la fecha de pago
4. ✅ Registra el usuario que marcó como pagada (del token JWT)
5. ✅ Guarda todos los datos de pago proporcionados
6. ✅ Actualiza `updatedAt` y `lastModifiedBy`
7. ✅ No permite marcar como pagada una factura ya pagada
8. ✅ No permite marcar como pagada una factura cancelada
9. ✅ No permite fechas de pago futuras

**Cuándo usar:**
- Cuando el cliente realiza un pago
- Cuando se recibe un comprobante de pago
- Cuando se confirma un depósito o transferencia

---

## Estructuras JSON

### Objeto Invoice (Factura)

```json
{
  "invoiceId": 1,
  "contractId": "CONT-001",
  "invoiceNumber": "INV-2024-001",
  "invoiceDate": "2024-01-15T00:00:00",
  "dueDate": "2024-02-15T00:00:00",
  "subTotal": 1000.00,
  "tax": 0.00,
  "total": 1000.00,
  "currencyCode": "USD",
  "status": "Draft",
  "paymentStatus": "Unpaid",
  "paidDate": null,
  "paidBy": null,
  "paymentMethodId": null,
  "paymentReference": null,
  "depositNumber": null,
  "transferNumber": null,
  "notes": null,
  "createdAt": "2024-01-15T10:00:00",
  "updatedAt": null,
  "lastModifiedBy": null
}
```

**Descripción de campos:**
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `invoiceId` | int | ID único de la factura |
| `contractId` | string | ID del contrato al que pertenece |
| `invoiceNumber` | string | Número de factura (INV-YYYY-###) |
| `invoiceDate` | DateTime | Fecha de emisión de la factura |
| `dueDate` | DateTime | Fecha de vencimiento |
| `subTotal` | decimal | Subtotal (sin impuestos) |
| `tax` | decimal | Impuestos (actualmente siempre 0) |
| `total` | decimal | Total a pagar (subTotal + tax) |
| `currencyCode` | string | Código de moneda (USD, EUR, CRC, etc.) |
| `status` | string | Estado de la factura (Draft, Sent, Paid, Overdue, Cancelled) |
| `paymentStatus` | string | Estado de pago (Unpaid, Paid) |
| `paidDate` | DateTime? | Fecha en que se pagó (null si no está pagada) |
| `paidBy` | int? | ID del usuario que marcó como pagada |
| `paymentMethodId` | int? | ID del método de pago usado |
| `paymentReference` | string? | Referencia del pago |
| `depositNumber` | string? | Número de depósito |
| `transferNumber` | string? | Número de transferencia |
| `notes` | string? | Notas adicionales |
| `createdAt` | DateTime | Fecha de creación |
| `updatedAt` | DateTime? | Fecha de última actualización |
| `lastModifiedBy` | string? | Usuario que modificó por última vez |

---

### Objeto InvoiceItem (Línea de Factura)

```json
{
  "invoiceItemId": 1,
  "invoiceId": 1,
  "contractServiceId": 15,
  "description": "Gestión de Redes Sociales",
  "quantity": 1.00,
  "unitPrice": 1000.00,
  "discount": 0.00,
  "lineTotal": 1000.00,
  "serviceOrder": 1,
  "createdAt": "2024-01-15T10:00:00"
}
```

**Descripción de campos:**
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `invoiceItemId` | int | ID único del item |
| `invoiceId` | int | ID de la factura a la que pertenece |
| `contractServiceId` | int? | ID del servicio del contrato (null si es monto fijo) |
| `description` | string | Descripción del servicio/item |
| `quantity` | decimal | Cantidad |
| `unitPrice` | decimal | Precio unitario |
| `discount` | decimal | Descuento aplicado |
| `lineTotal` | decimal | Total de la línea (quantity * unitPrice - discount) |
| `serviceOrder` | int? | Orden del servicio |
| `createdAt` | DateTime | Fecha de creación |

---

### Objeto InvoiceAttachment (Adjunto)

```json
{
  "invoiceAttachmentId": 1,
  "invoiceId": 1,
  "fileUrl": "https://storage.example.com/invoices/comprobante-001.pdf",
  "fileName": "comprobante-pago-enero.pdf",
  "fileType": "application/pdf",
  "fileSize": 245678,
  "uploadedBy": 3,
  "uploadedAt": "2024-01-25T16:50:00"
}
```

**Descripción de campos:**
| Campo | Tipo | Descripción |
|-------|------|-------------|
| `invoiceAttachmentId` | int | ID único del adjunto |
| `invoiceId` | int | ID de la factura |
| `fileUrl` | string | URL del archivo almacenado |
| `fileName` | string | Nombre del archivo |
| `fileType` | string | Tipo MIME del archivo |
| `fileSize` | long | Tamaño en bytes |
| `uploadedBy` | int | ID del usuario que subió el archivo |
| `uploadedAt` | DateTime | Fecha de carga |

---

## Flujos de Trabajo

### Flujo 1: Crear Contrato y Generar Facturas

```
1. Usuario crea un nuevo contrato
   └─> POST /api/corporate/contract
   
2. Sistema muestra el contrato creado con botón "Generar Facturas"
   
3. Usuario presiona "Generar Facturas"
   └─> POST /api/corporate/invoice/generate/{contractId}
   
4. Sistema valida y genera facturas
   ├─> Si éxito: Muestra mensaje "Se generaron X facturas"
   └─> Si ya existen: Muestra "Ya existen facturas para este contrato"
   
5. Sistema recarga la lista de facturas
   └─> GET /api/corporate/invoice?contractId={contractId}
```

### Flujo 2: Ver Facturas de un Contrato

```
1. Usuario entra a detalle de contrato
   
2. Sistema carga resumen de facturas
   └─> GET /api/corporate/invoice/summary/{contractId}
   └─> Muestra: "12 facturas | 5 pagadas | 7 pendientes | 2 vencidas"
   
3. Sistema carga lista de facturas
   └─> GET /api/corporate/invoice?contractId={contractId}&pageNumber=1&pageSize=10
   
4. Usuario puede filtrar por estado
   └─> GET /api/corporate/invoice?contractId={contractId}&status=Unpaid
   
5. Usuario puede ordenar
   └─> GET /api/corporate/invoice?contractId={contractId}&orderBy=DueDate
```

### Flujo 3: Marcar Factura como Pagada

```
1. Usuario selecciona una factura de la lista
   
2. Usuario hace clic en "Marcar como Pagada"
   
3. Sistema muestra modal/formulario con campos:
   - Fecha de pago (por defecto: hoy)
   - Método de pago (dropdown)
   - Referencia de pago
   - Número de depósito
   - Número de transferencia
   
4. Usuario llena los datos y confirma
   └─> PUT /api/corporate/invoice/{invoiceId}/mark-as-paid
   └─> Body: { paidDate, paymentMethodId, paymentReference, ... }
   
5. Sistema confirma y actualiza la lista
   ├─> Si éxito: Muestra "Factura marcada como pagada"
   ├─> Actualiza el estado visual de la factura
   └─> Recarga el resumen de facturas
```

### Flujo 4: Ver Detalle de Factura

```
1. Usuario hace clic en una factura de la lista
   
2. Sistema carga el detalle completo
   └─> GET /api/corporate/invoice/{invoiceId}
   
3. Sistema muestra:
   ├─> Datos de la factura (número, fechas, montos)
   ├─> Lista de items/servicios incluidos
   ├─> Adjuntos (comprobantes de pago)
   └─> Botones de acción según estado
       ├─> Si Unpaid: "Marcar como Pagada"
       ├─> Si Paid: "Ver Comprobante"
       └─> "Imprimir" / "Descargar PDF"
```

### Flujo 5: Buscar Facturas Vencidas

```
1. Usuario entra a dashboard o sección de facturas
   
2. Sistema carga facturas vencidas
   └─> GET /api/corporate/invoice?isOverdue=true&orderBy=DueDate
   
3. Sistema muestra alerta/badge con cantidad de vencidas
   └─> "⚠️ Tienes 5 facturas vencidas"
   
4. Usuario puede filtrar por contrato específico
   └─> GET /api/corporate/invoice?isOverdue=true&contractId={contractId}
```

---

## Casos de Uso

### Caso de Uso 1: Contrato Mensual Simple

**Datos del Contrato:**
- ContractId: "CONT-001"
- StartDate: 2024-01-15
- EndDate: 2024-12-15
- PaymentFrequency: "Monthly"
- PaymentDay: 15
- FeeAmount: 12000.00
- CurrencyCode: "USD"

**Resultado al generar facturas:**
- Se generan 12 facturas
- Cada factura por $1,000.00
- Fechas de emisión: 15 de cada mes
- Fechas de vencimiento: 15 del mes siguiente
- Números: INV-2024-001 a INV-2024-012

**Request:**
```
POST /api/corporate/invoice/generate/CONT-001
```

**Response:**
```json
{
  "success": true,
  "message": "Se generaron 12 facturas exitosamente",
  "data": {
    "invoicesGenerated": 12,
    "totalAmount": 12000.00,
    "invoiceIds": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]
  }
}
```

---

### Caso de Uso 2: Contrato Trimestral con Servicios

**Datos del Contrato:**
- ContractId: "CONT-002"
- StartDate: 2024-01-01
- EndDate: 2024-12-31
- PaymentFrequency: "Quarterly"
- PaymentDay: 1
- FeeAmount: null
- ContractServices:
  - Servicio 1: $6,000.00
  - Servicio 2: $3,000.00
- Total: $9,000.00
- CurrencyCode: "USD"

**Resultado al generar facturas:**
- Se generan 4 facturas
- Cada factura por $2,250.00
- Cada factura tiene 2 items (uno por servicio)
- Fechas: 01-Ene, 01-Abr, 01-Jul, 01-Oct

**Request:**
```
POST /api/corporate/invoice/generate/CONT-002
```

**Response:**
```json
{
  "success": true,
  "message": "Se generaron 4 facturas exitosamente",
  "data": {
    "invoicesGenerated": 4,
    "totalAmount": 9000.00,
    "invoiceIds": [13, 14, 15, 16]
  }
}
```

---

### Caso de Uso 3: Intentar Generar Facturas Duplicadas

**Escenario:** El usuario presiona el botón "Generar Facturas" dos veces.

**Request 1 (primera vez):**
```
POST /api/corporate/invoice/generate/CONT-001
```

**Response 1:**
```json
{
  "success": true,
  "message": "Se generaron 12 facturas exitosamente",
  "data": {
    "invoicesGenerated": 12,
    "totalAmount": 12000.00
  }
}
```

**Request 2 (segunda vez):**
```
POST /api/corporate/invoice/generate/CONT-001
```

**Response 2:**
```json
{
  "success": false,
  "message": "Ya existen facturas generadas para este contrato",
  "data": {
    "success": false,
    "invoicesGenerated": 0,
    "existingInvoicesCount": 12
  }
}
```

**Comportamiento esperado en UI:**
- Mostrar mensaje de advertencia
- Deshabilitar el botón "Generar Facturas"
- Mostrar botón "Ver Facturas" en su lugar

---

### Caso de Uso 4: Marcar Múltiples Facturas como Pagadas

**Escenario:** El cliente pagó 3 facturas con una sola transferencia.

**Request 1:**
```
PUT /api/corporate/invoice/1/mark-as-paid
Body:
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 2,
  "paymentReference": "TRANSFER-BATCH-001",
  "transferNumber": "TRF-98765"
}
```

**Request 2:**
```
PUT /api/corporate/invoice/2/mark-as-paid
Body:
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 2,
  "paymentReference": "TRANSFER-BATCH-001",
  "transferNumber": "TRF-98765"
}
```

**Request 3:**
```
PUT /api/corporate/invoice/3/mark-as-paid
Body:
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 2,
  "paymentReference": "TRANSFER-BATCH-001",
  "transferNumber": "TRF-98765"
}
```

**Comportamiento esperado en UI:**
- Permitir selección múltiple de facturas
- Mostrar formulario de pago una sola vez
- Aplicar el mismo pago a todas las seleccionadas
- Hacer las llamadas en secuencia o paralelo

---

## Validaciones y Errores

### Validaciones del Sistema

#### Al Generar Facturas:
| Validación | Mensaje de Error |
|------------|------------------|
| Contrato no existe | "Contrato con ID {id} no encontrado" |
| Ya existen facturas | "Ya existen facturas generadas para este contrato" |
| Falta StartDate | "El contrato debe tener StartDate" |
| Falta EndDate | "El contrato debe tener EndDate" |
| Falta PaymentFrequency | "El contrato debe tener PaymentFrequency" |
| Falta CurrencyCode | "El contrato debe tener CurrencyCode" |
| Sin monto ni servicios | "El contrato debe tener FeeAmount o ContractServices activos" |
| Monto total = 0 | "El monto total del contrato debe ser mayor a cero" |

#### Al Marcar como Pagada:
| Validación | Mensaje de Error |
|------------|------------------|
| Factura no existe | "Factura con ID {id} no encontrada" |
| Ya está pagada | "La factura ya está pagada" |
| Está cancelada | "No se puede pagar una factura cancelada" |
| Fecha futura | "La fecha de pago no puede ser futura" |

---

### Códigos de Estado HTTP

| Código | Significado | Cuándo se usa |
|--------|-------------|---------------|
| 200 | OK | Operación exitosa |
| 400 | Bad Request | Error de validación o datos incorrectos |
| 401 | Unauthorized | Token JWT inválido o expirado |
| 404 | Not Found | Recurso no encontrado (contrato o factura) |
| 500 | Internal Server Error | Error del servidor |

---

### Manejo de Errores en Frontend

#### Estructura de Error Estándar:
```json
{
  "success": false,
  "message": "Descripción del error",
  "data": null
}
```

#### Recomendaciones:
1. **Siempre verificar `success`** antes de procesar `data`
2. **Mostrar `message`** al usuario en caso de error
3. **Manejar errores 401** redirigiendo a login
4. **Manejar errores 500** mostrando mensaje genérico
5. **Validar en frontend** antes de enviar (fecha no futura, campos requeridos)

---

## Consideraciones Importantes

### 1. Autenticación
- **Todos los endpoints requieren token JWT**
- El token se obtiene del endpoint de login
- Incluir en header: `Authorization: Bearer {token}`
- Si el token expira (401), redirigir a login

### 2. Formato de Fechas
- **Enviar:** Formato ISO 8601: `YYYY-MM-DD` o `YYYY-MM-DDTHH:mm:ss`
- **Recibir:** Formato ISO 8601 con timezone
- **Ejemplo:** `2024-01-15T00:00:00`

### 3. Monedas
- Los montos siempre están en la moneda del contrato (`currencyCode`)
- Formato: decimal con 2 decimales
- **Ejemplo:** `1000.00`, `2250.50`

### 4. Paginación
- Si no se envían `pageNumber` y `pageSize`, devuelve todos los registros
- `pageNumber` inicia en 1 (no en 0)
- `pageSize` recomendado: 10, 20 o 50
- Usar `totalPages` para mostrar paginador

### 5. Estados de Factura
- **Draft:** Recién generada, no enviada al cliente
- **Sent:** Enviada al cliente (futuro)
- **Paid:** Pagada completamente
- **Overdue:** Vencida (DueDate < hoy y Unpaid)
- **Cancelled:** Cancelada (futuro)

### 6. Comportamiento del Botón "Generar Facturas"
- **Mostrar** si: No existen facturas para el contrato
- **Ocultar/Deshabilitar** si: Ya existen facturas
- **Validar** antes de habilitar: Contrato tiene datos completos

### 7. Indicadores Visuales Recomendados
- **Badge verde:** Facturas pagadas
- **Badge amarillo:** Facturas pendientes
- **Badge rojo:** Facturas vencidas
- **Icono de alerta:** Si hay facturas vencidas
- **Barra de progreso:** % de facturas pagadas

### 8. Acciones Recomendadas por Estado
| Estado | Acciones Disponibles |
|--------|---------------------|
| Draft | Ver, Marcar como Pagada, Enviar (futuro) |
| Sent | Ver, Marcar como Pagada |
| Paid | Ver, Ver Comprobante, Imprimir |
| Overdue | Ver, Marcar como Pagada, Enviar Recordatorio |
| Cancelled | Ver (solo lectura) |

---

## Ejemplos de Interfaces Recomendadas

### Pantalla 1: Detalle de Contrato

```
┌─────────────────────────────────────────────────────┐
│ Contrato: CONT-001                                  │
│ Cliente: Empresa XYZ                                │
│ Período: 15 Ene 2024 - 15 Dic 2024                 │
│ Monto Total: $12,000.00 USD                         │
│                                                     │
│ ┌─────────────────────────────────────────────┐   │
│ │ 📊 Resumen de Facturas                      │   │
│ │                                             │   │
│ │ Total: 12 facturas                          │   │
│ │ ✅ Pagadas: 5 ($5,000.00)                   │   │
│ │ ⏳ Pendientes: 7 ($7,000.00)                │   │
│ │ ⚠️  Vencidas: 2 ($2,000.00)                 │   │
│ │                                             │   │
│ │ [Ver Todas las Facturas]                    │   │
│ └─────────────────────────────────────────────┘   │
│                                                     │
│ [Generar Facturas] (si no existen)                 │
└─────────────────────────────────────────────────────┘
```

### Pantalla 2: Lista de Facturas

```
┌─────────────────────────────────────────────────────────────────┐
│ Facturas del Contrato CONT-001                                  │
│                                                                 │
│ Filtros: [Estado ▼] [Fecha ▼] [Buscar...]                     │
│                                                                 │
│ ┌───────────────────────────────────────────────────────────┐ │
│ │ INV-2024-001 | 15 Ene 2024 | Vence: 15 Feb | $1,000.00   │ │
│ │ [⚠️ Vencida] [Marcar como Pagada]                         │ │
│ └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ┌───────────────────────────────────────────────────────────┐ │
│ │ INV-2024-002 | 15 Feb 2024 | Vence: 15 Mar | $1,000.00   │ │
│ │ [✅ Pagada] [Ver Detalle]                                  │ │
│ └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ┌───────────────────────────────────────────────────────────┐ │
│ │ INV-2024-003 | 15 Mar 2024 | Vence: 15 Abr | $1,000.00   │ │
│ │ [⏳ Pendiente] [Marcar como Pagada]                        │ │
│ └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Página 1 de 4                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Pantalla 3: Modal "Marcar como Pagada"

```
┌─────────────────────────────────────────┐
│ Marcar Factura como Pagada              │
│                                         │
│ Factura: INV-2024-001                   │
│ Monto: $1,000.00 USD                    │
│                                         │
│ Fecha de Pago: [15/02/2024] 📅         │
│                                         │
│ Método de Pago: [Transferencia ▼]      │
│                                         │
│ Referencia: [________________]          │
│                                         │
│ Número de Transferencia: [_________]    │
│                                         │
│ Número de Depósito: [_________]         │
│                                         │
│ [Cancelar]  [Confirmar Pago]           │
└─────────────────────────────────────────┘
```

---

## Preguntas Frecuentes

### ¿Qué pasa si el usuario genera facturas dos veces?
El sistema previene duplicados. La segunda vez retorna un mensaje indicando que ya existen facturas.

### ¿Se pueden editar facturas después de generarlas?
No, las facturas son inmutables. Si hay un error, se deben eliminar y regenerar (funcionalidad futura).

### ¿Se pueden pagar facturas parcialmente?
No, las facturas se pagan completamente o no se pagan. No hay pagos parciales.

### ¿Qué pasa si se modifica el contrato después de generar facturas?
Las facturas NO se regeneran automáticamente. Mantienen los montos originales.

### ¿Cómo se marcan las facturas como vencidas?
Automáticamente por un job del backend cuando `DueDate < hoy` y `PaymentStatus = Unpaid`.

### ¿Se pueden adjuntar comprobantes de pago?
Sí, pero esa funcionalidad está pendiente de implementar (endpoint futuro).

### ¿Se pueden enviar facturas por email?
Funcionalidad futura. Por ahora solo se generan en estado `Draft`.

---

## Contacto y Soporte

Para dudas o aclaraciones sobre la API, contactar al equipo de backend.

**Endpoints de prueba disponibles en:**
- Desarrollo: `http://localhost:5009/swagger`
- Producción: `https://api.vhconsultor.com/swagger`

---

**Versión:** 1.0  
**Fecha:** Diciembre 2024  
**Autor:** Equipo Backend VHConsultor

