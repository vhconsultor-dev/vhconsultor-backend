# Cambios en Estructura de Facturas y Documentación de APIs

## 📋 Resumen de Cambios

### Corrección de Estructura InvoiceAttachment

**Problema identificado:**
El código intentaba usar columnas (`FileName`, `FileType`, `FileSize`) que no existen en la tabla `[Corporate].[InvoiceAttachments]` de la base de datos.

**Solución implementada:**
- Se eliminaron las propiedades `FileName`, `FileType`, `FileSize` de la entidad `InvoiceAttachment`
- Se actualizaron las consultas SQL para usar solo las columnas existentes
- Se modificaron los comandos de upload/delete para trabajar sin esas propiedades
- `UploadedBy` ahora es `int` requerido (no nullable) para coincidir con la BD

**Archivos modificados:**
- `ModelLayer/Corporate/Entities/InvoiceAttachment.cs`
- `ModelLayer/DBcontext.cs`
- `BusinessLayer/Corporate/Queries/InvoiceQueryRepository.cs`
- `BusinessLayer/Corporate/Commands/UploadInvoiceAttachmentCommand.cs`
- `BusinessLayer/Corporate/Commands/DeleteInvoiceAttachmentCommand.cs`

---

## 📊 Estructura de Datos de Facturas

### 1. Entidad Invoice (Factura)

**Tabla:** `[Corporate].[Invoices]`

| Campo | Tipo | Nullable | Descripción |
|-------|------|----------|-------------|
| `InvoiceId` | `int` | No | ID único de la factura (PK, Identity) |
| `ContractId` | `int` | No | ID del contrato asociado (FK) |
| `InvoiceNumber` | `string` | No | Número de factura (formato: INV-YYYY-XXX) |
| `InvoiceDate` | `DateTime` | No | Fecha de emisión de la factura |
| `DueDate` | `DateTime` | No | Fecha de vencimiento |
| `SubTotal` | `decimal(18,2)` | No | Subtotal antes de impuestos |
| `Tax` | `decimal(18,2)` | No | Monto de impuestos |
| `Total` | `decimal(18,2)` | No | Total a pagar (SubTotal + Tax) |
| `CurrencyCode` | `string` (char(3)) | No | Código de moneda (ej: USD, CRC) |
| `Status` | `string` (nvarchar(50)) | No | Estado de la factura: `Draft`, `Sent`, `Paid`, `Overdue`, `Cancelled` |
| `PaymentStatus` | `string` (nvarchar(50)) | No | Estado de pago: `Unpaid`, `Paid` |
| `PaidDate` | `DateTime?` | Sí | Fecha en que se marcó como pagada |
| `PaidBy` | `int?` | Sí | ID del usuario que marcó como pagada (FK) |
| `PaymentMethodId` | `int?` | Sí | ID del método de pago utilizado (FK) |
| `PaymentReference` | `string` (nvarchar(255)) | Sí | Referencia del pago |
| `DepositNumber` | `string` (nvarchar(100)) | Sí | Número de depósito |
| `TransferNumber` | `string` (nvarchar(100)) | Sí | Número de transferencia |
| `Notes` | `string` (nvarchar(max)) | Sí | Notas adicionales |
| `CreatedAt` | `DateTime` | No | Fecha de creación |
| `UpdatedAt` | `DateTime?` | Sí | Fecha de última actualización |
| `LastModifiedBy` | `string` (nvarchar(255)) | Sí | Usuario que realizó la última modificación |

### 2. Entidad InvoiceItem (Item de Factura)

**Tabla:** `[Corporate].[InvoiceItems]`

| Campo | Tipo | Nullable | Descripción |
|-------|------|----------|-------------|
| `InvoiceItemId` | `int` | No | ID único del item (PK, Identity) |
| `InvoiceId` | `int` | No | ID de la factura asociada (FK) |
| `ContractServiceId` | `int?` | Sí | ID del servicio del contrato asociado (FK) |
| `Description` | `string` (nvarchar(1000)) | No | Descripción del item |
| `Quantity` | `decimal(10,2)` | No | Cantidad (default: 1) |
| `UnitPrice` | `decimal(15,2)` | No | Precio unitario |
| `Discount` | `decimal(15,2)` | No | Descuento aplicado (default: 0) |
| `LineTotal` | `decimal(15,2)` | No | Total de la línea (Quantity * UnitPrice - Discount) |
| `ServiceOrder` | `int?` | Sí | Orden del servicio |
| `CreatedAt` | `DateTime` | No | Fecha de creación |

### 3. Entidad InvoiceAttachment (Adjunto de Factura)

**Tabla:** `[Corporate].[InvoiceAttachments]`

| Campo | Tipo | Nullable | Descripción |
|-------|------|----------|-------------|
| `InvoiceAttachmentId` | `int` | No | ID único del adjunto (PK, Identity) |
| `InvoiceId` | `int` | No | ID de la factura asociada (FK) |
| `FileUrl` | `string` (nvarchar(500)) | No | URL completa del archivo en Azure Blob Storage |
| `UploadedBy` | `int` | No | ID del usuario que subió el archivo (FK) |
| `UploadedAt` | `DateTime` | No | Fecha y hora de carga |

**⚠️ Nota importante:** 
- `FileName`, `FileType`, `FileSize` **NO existen** en la base de datos
- El nombre del archivo debe extraerse de la URL si es necesario
- La información del archivo original se obtiene del `IFormFile` durante la carga

---

## 🔌 Endpoints de API

### GET /api/corporate/Invoice

**Descripción:** Obtiene lista de facturas con filtros opcionales y paginación.

**Query Parameters:**
- `InvoiceId` (int?, opcional): Filtrar por ID de factura
- `ContractId` (int?, opcional): Filtrar por ID de contrato
- `InvoiceNumber` (string?, opcional): Buscar por número de factura (LIKE)
- `Status` (string?, opcional): Filtrar por estado (Draft, Sent, Paid, Overdue, Cancelled)
- `PaymentStatus` (string?, opcional): Filtrar por estado de pago (Unpaid, Paid)
- `CurrencyCode` (string?, opcional): Filtrar por código de moneda
- `InvoiceDateFrom` (DateTime?, opcional): Rango de fechas de emisión (desde)
- `InvoiceDateTo` (DateTime?, opcional): Rango de fechas de emisión (hasta)
- `DueDateFrom` (DateTime?, opcional): Rango de fechas de vencimiento (desde)
- `DueDateTo` (DateTime?, opcional): Rango de fechas de vencimiento (hasta)
- `IsOverdue` (bool?, opcional): Filtrar facturas vencidas (true)
- `OrderBy` (string?, opcional): Ordenamiento (InvoiceDate, DueDate, Total, Status)
- `PageNumber` (int?, opcional): Número de página
- `PageSize` (int?, opcional): Tamaño de página

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "invoices": [
      {
        "invoiceId": 1,
        "contractId": 5,
        "invoiceNumber": "INV-2025-001",
        "invoiceDate": "2025-01-15T00:00:00",
        "dueDate": "2025-02-15T00:00:00",
        "subTotal": 1000.00,
        "tax": 130.00,
        "total": 1130.00,
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
        "createdAt": "2025-01-15T10:30:00",
        "updatedAt": null,
        "lastModifiedBy": null
      }
    ],
    "totalRecords": 25,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 3
  },
  "message": "Invoices retrieved successfully",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### GET /api/corporate/Invoice/{invoiceId}

**Descripción:** Obtiene una factura específica con sus items y adjuntos.

**Path Parameters:**
- `invoiceId` (int, requerido): ID de la factura

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "invoice": {
      "invoiceId": 1,
      "contractId": 5,
      "invoiceNumber": "INV-2025-001",
      "invoiceDate": "2025-01-15T00:00:00",
      "dueDate": "2025-02-15T00:00:00",
      "subTotal": 1000.00,
      "tax": 130.00,
      "total": 1130.00,
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
      "createdAt": "2025-01-15T10:30:00",
      "updatedAt": null,
      "lastModifiedBy": null
    },
    "items": [
      {
        "invoiceItemId": 1,
        "invoiceId": 1,
        "contractServiceId": 10,
        "description": "Servicio de consultoría mensual",
        "quantity": 1.00,
        "unitPrice": 1000.00,
        "discount": 0.00,
        "lineTotal": 1000.00,
        "serviceOrder": 1,
        "createdAt": "2025-01-15T10:30:00"
      }
    ],
    "attachments": [
      {
        "invoiceAttachmentId": 1,
        "invoiceId": 1,
        "fileUrl": "https://vhstorageblob.blob.core.windows.net/vh-container/InvoiceAttachments/INV-2025-001/abc123.pdf",
        "uploadedBy": 2,
        "uploadedAt": "2025-01-20T14:30:00"
      }
    ]
  },
  "message": "Invoice retrieved successfully",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### POST /api/corporate/Invoice/generate/{contractId}

**Descripción:** Genera todas las facturas automáticamente para un contrato.

**Path Parameters:**
- `contractId` (int, requerido): ID del contrato

**Response JSON (Éxito):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Successfully generated 12 invoices for contract ID 5",
    "contractId": 5,
    "invoicesGenerated": 12,
    "invoiceNumbers": [
      "INV-2025-001",
      "INV-2025-002",
      "INV-2025-003"
    ]
  },
  "message": "Successfully generated 12 invoices for contract ID 5",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

**Response JSON (Ya existen facturas):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": {
    "success": false,
    "message": "Invoices already exist for contract ID 5. Found 12 existing invoices: INV-2025-001, INV-2025-002, ...",
    "contractId": 5,
    "invoicesGenerated": 0,
    "invoiceNumbers": []
  },
  "message": "Invoices already exist for contract ID 5. Found 12 existing invoices: INV-2025-001, INV-2025-002, ...",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### PUT /api/corporate/Invoice/{invoiceId}/mark-as-paid

**Descripción:** Marca una factura como pagada.

**Path Parameters:**
- `invoiceId` (int, requerido): ID de la factura

**Request Body:**
```json
{
  "paidDate": "2025-01-20T00:00:00",
  "paymentMethodId": 1,
  "paymentReference": "REF-12345",
  "depositNumber": "DEP-67890",
  "transferNumber": "TRF-11111"
}
```

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Invoice 'INV-2025-001' has been successfully marked as paid",
    "invoiceId": 1,
    "invoiceNumber": "INV-2025-001",
    "paidDate": "2025-01-20T00:00:00"
  },
  "message": "Invoice 'INV-2025-001' has been successfully marked as paid",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### POST /api/corporate/Invoice/{invoiceId}/attachments

**Descripción:** Sube un archivo adjunto a una factura.

**Path Parameters:**
- `invoiceId` (int, requerido): ID de la factura

**Request:** `multipart/form-data`
- `file` (IFormFile, requerido): Archivo a subir

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "File 'comprobante.pdf' has been successfully uploaded to invoice 'INV-2025-001'",
    "attachmentId": 1,
    "fileName": "comprobante.pdf",
    "fileUrl": "https://vhstorageblob.blob.core.windows.net/vh-container/InvoiceAttachments/INV-2025-001/abc123.pdf",
    "fileSize": 245760,
    "uploadedAt": "2025-01-20T14:30:00"
  },
  "message": "File 'comprobante.pdf' has been successfully uploaded to invoice 'INV-2025-001'",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### DELETE /api/corporate/Invoice/attachments/{attachmentId}

**Descripción:** Elimina un adjunto específico de una factura.

**Path Parameters:**
- `attachmentId` (int, requerido): ID del adjunto

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Attachment 'comprobante.pdf' has been successfully deleted from invoice 'INV-2025-001'",
    "deletedFileName": "comprobante.pdf",
    "invoiceNumber": "INV-2025-001",
    "deletedCount": 1
  },
  "message": "Attachment 'comprobante.pdf' has been successfully deleted from invoice 'INV-2025-001'",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### DELETE /api/corporate/Invoice/{invoiceId}/attachments

**Descripción:** Elimina todos los adjuntos de una factura.

**Path Parameters:**
- `invoiceId` (int, requerido): ID de la factura

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Successfully deleted 3 attachment(s) from invoice 'INV-2025-001'",
    "deletedFileName": null,
    "invoiceNumber": "INV-2025-001",
    "deletedCount": 3
  },
  "message": "Successfully deleted 3 attachment(s) from invoice 'INV-2025-001'",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

### DELETE /api/corporate/Invoice

**Descripción:** Elimina facturas (individuales o todas de un contrato).

**Query Parameters:**
- `InvoiceIds` (int[], opcional): Lista de IDs de facturas a eliminar
- `ContractId` (int?, opcional): ID del contrato (elimina todas las facturas del contrato)

**Request Body (para múltiples facturas):**
```json
{
  "invoiceIds": [1, 2, 3]
}
```

**Request Body (para todas las facturas de un contrato):**
```json
{
  "contractId": 5
}
```

**Response JSON:**
```json
{
  "status": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "message": "Successfully deleted 3 invoice(s): INV-2025-001, INV-2025-002, INV-2025-003",
    "deletedInvoiceCount": 3
  },
  "message": "Successfully deleted 3 invoice(s): INV-2025-001, INV-2025-002, INV-2025-003",
  "errorNumber": null,
  "timestamp": "2025-12-23T17:30:00"
}
```

---

## 📝 Mapeo de Campos para Frontend

### Invoice (Factura)

```typescript
interface Invoice {
  invoiceId: number;
  contractId: number;
  invoiceNumber: string;
  invoiceDate: string; // ISO 8601 DateTime
  dueDate: string; // ISO 8601 DateTime
  subTotal: number;
  tax: number;
  total: number;
  currencyCode: string; // "USD", "CRC", etc.
  status: "Draft" | "Sent" | "Paid" | "Overdue" | "Cancelled";
  paymentStatus: "Unpaid" | "Paid";
  paidDate: string | null; // ISO 8601 DateTime
  paidBy: number | null;
  paymentMethodId: number | null;
  paymentReference: string | null;
  depositNumber: string | null;
  transferNumber: string | null;
  notes: string | null;
  createdAt: string; // ISO 8601 DateTime
  updatedAt: string | null; // ISO 8601 DateTime
  lastModifiedBy: string | null;
}
```

### InvoiceItem (Item de Factura)

```typescript
interface InvoiceItem {
  invoiceItemId: number;
  invoiceId: number;
  contractServiceId: number | null;
  description: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  lineTotal: number;
  serviceOrder: number | null;
  createdAt: string; // ISO 8601 DateTime
}
```

### InvoiceAttachment (Adjunto de Factura)

```typescript
interface InvoiceAttachment {
  invoiceAttachmentId: number;
  invoiceId: number;
  fileUrl: string; // URL completa en Azure Blob Storage
  uploadedBy: number;
  uploadedAt: string; // ISO 8601 DateTime
}
```

### InvoiceDetailDto (Respuesta de GET /Invoice/{id})

```typescript
interface InvoiceDetailDto {
  invoice: Invoice;
  items: InvoiceItem[];
  attachments: InvoiceAttachment[];
}
```

### InvoiceQueryResult (Respuesta de GET /Invoice con paginación)

```typescript
interface InvoiceQueryResult {
  invoices: Invoice[];
  totalRecords: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number; // Calculado automáticamente
}
```

---

## ⚠️ Notas Importantes para Frontend

1. **InvoiceAttachment - FileName:**
   - El campo `fileName` **NO existe** en la respuesta del API
   - Para obtener el nombre del archivo, extraerlo de `fileUrl`:
     ```typescript
     const fileName = fileUrl.split('/').pop() || 'Unknown file';
     ```

2. **Estados de Factura:**
   - `Status`: `Draft`, `Sent`, `Paid`, `Overdue`, `Cancelled`
   - `PaymentStatus`: `Unpaid`, `Paid`
   - Una factura puede tener `Status = "Paid"` y `PaymentStatus = "Paid"` cuando está pagada

3. **Fechas:**
   - Todas las fechas vienen en formato ISO 8601 (ej: `"2025-01-15T00:00:00"`)
   - Algunas fechas pueden ser `null` (usar `string | null` en TypeScript)

4. **Monedas:**
   - `CurrencyCode` es un string de 3 caracteres (ej: "USD", "CRC")
   - Todos los montos (`SubTotal`, `Tax`, `Total`, `UnitPrice`, `LineTotal`) son `decimal` con 2 decimales

5. **Paginación:**
   - `totalPages` se calcula automáticamente: `Math.ceil(totalRecords / pageSize)`
   - Si no se envía paginación, se retornan todos los registros

6. **Adjuntos:**
   - `fileUrl` es la URL completa del archivo en Azure Blob Storage
   - Puede usarse directamente para descargar o mostrar el archivo
   - El nombre del archivo debe extraerse de la URL si se necesita

---

## 🔄 Cambios en Estructura de Respuestas

### Antes (Incorrecto):
```json
{
  "attachments": [
    {
      "invoiceAttachmentId": 1,
      "invoiceId": 1,
      "fileUrl": "...",
      "fileName": "archivo.pdf",  // ❌ NO existe en BD
      "fileType": "application/pdf",  // ❌ NO existe en BD
      "fileSize": 245760,  // ❌ NO existe en BD
      "uploadedBy": 2,
      "uploadedAt": "2025-01-20T14:30:00"
    }
  ]
}
```

### Ahora (Correcto):
```json
{
  "attachments": [
    {
      "invoiceAttachmentId": 1,
      "invoiceId": 1,
      "fileUrl": "https://vhstorageblob.blob.core.windows.net/vh-container/InvoiceAttachments/INV-2025-001/abc123.pdf",
      "uploadedBy": 2,
      "uploadedAt": "2025-01-20T14:30:00"
    }
  ]
}
```

**Para obtener el nombre del archivo en frontend:**
```typescript
const fileName = attachment.fileUrl.split('/').pop() || 'Unknown file';
```

---

## ✅ Validaciones Importantes

1. **Al eliminar facturas:**
   - No se pueden eliminar facturas con `PaymentStatus = "Paid"`
   - No se pueden eliminar facturas que tengan adjuntos
   - Primero eliminar adjuntos, luego facturas

2. **Al marcar como pagada:**
   - La factura no debe estar ya pagada
   - La factura no debe estar cancelada
   - La fecha de pago no puede ser futura

3. **Al subir adjuntos:**
   - Validación de tipo de archivo (según configuración)
   - Validación de tamaño máximo (según configuración)
   - El archivo se guarda en Azure Blob Storage

---

## 📌 Endpoints Disponibles - Resumen

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/corporate/Invoice` | Lista de facturas con filtros |
| `GET` | `/api/corporate/Invoice/{id}` | Detalle de factura con items y adjuntos |
| `POST` | `/api/corporate/Invoice/generate/{contractId}` | Generar facturas de un contrato |
| `PUT` | `/api/corporate/Invoice/{id}/mark-as-paid` | Marcar factura como pagada |
| `POST` | `/api/corporate/Invoice/{id}/attachments` | Subir adjunto |
| `DELETE` | `/api/corporate/Invoice/attachments/{attachmentId}` | Eliminar adjunto específico |
| `DELETE` | `/api/corporate/Invoice/{id}/attachments` | Eliminar todos los adjuntos |
| `DELETE` | `/api/corporate/Invoice` | Eliminar facturas |

---

**Última actualización:** 2025-12-23

