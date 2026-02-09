# Billing Report API – Documentación detallada

Documentación del API de reportes de facturación para alimentar una pantalla de reportería. Se describe el funcionamiento completo del endpoint, parámetros, filtros (incluyendo búsquedas tipo LIKE), criterios de ordenamiento y estructura de las respuestas JSON.

---

## 1. Información general del endpoint

| Concepto | Valor |
|----------|--------|
| **URL del API** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/reporting/billing/invoices` |
| **Método HTTP** | `GET` |
| **Ruta base** | `api/corporate/reporting/billing` |
| **Ruta completa del recurso** | `GET api/corporate/reporting/billing/invoices` |
| **Autenticación** | Requerida (`[Authorize]`). Debe enviarse el token o credenciales que exija la API. |
| **Tipo de contenido** | Respuesta en JSON. |

**Propósito:** Obtener un listado de facturas con filtros opcionales para distintos escenarios de reporte: todas las facturas, pendientes de pago, vencidas, pagadas, canceladas, borrador, enviadas, y combinado con búsquedas por cliente, contrato y número de factura.

---

## 2. Parámetros de consulta (query parameters)

Todos los parámetros son **opcionales**. Si no se envía ninguno, el API devuelve **todas** las facturas (sin filtrar por estado), ordenadas por fecha de factura descendente (más recientes primero).

Cuando se envían **varios parámetros a la vez**, el servidor aplica **lógica AND**: una factura solo aparece si cumple **todos** los criterios indicados.

| Parámetro | Tipo | Descripción breve |
|-----------|------|-------------------|
| `invoiceStatusType` | string | Tipo de estado para filtrar facturas (all, pending, overdue, paid, cancelled, draft, sent). |
| `customerTaxId` | string | NIT / cédula del cliente; búsqueda parcial (LIKE). |
| `customerName` | string | Nombre del cliente (CompanyName); búsqueda parcial (LIKE). |
| `feeTypeId` | integer | Tipo de tarifa: 1 = monto fijo, 2 = porcentaje. |
| `contractNumber` | string | Número de contrato; búsqueda parcial (LIKE). |
| `invoiceNumber` | string | Número de factura; búsqueda parcial (LIKE). |

---

## 3. Parámetro `invoiceStatusType` (filtro por tipo de estado)

Define **qué conjunto de facturas** se devuelve según estado de pago y/o estado de documento. El valor se compara en **minúsculas** (el API hace `Trim` y `ToLowerInvariant` internamente).

| Valor enviado | Comportamiento en base de datos | Uso típico en reportería |
|---------------|---------------------------------|----------------------------|
| *(omitido o vacío)* | No se aplica filtro por estado. Se devuelven **todas** las facturas. | Vista “Todas las facturas”. |
| `all` | Igual que omitir: todas las facturas. | Vista explícita “Todas”. |
| `pending` | `PaymentStatus = 'Unpaid'` y la factura no está cancelada (`Status` distinto de `'Cancelled'` o NULL). | Facturas pendientes de cobro (aún no vencidas + vencidas). |
| `overdue` | Igual que `pending` y además `DueDate < fecha/hora actual` (servidor). | Facturas vencidas (no pagadas y con fecha de vencimiento pasada). |
| `paid` | `PaymentStatus = 'Paid'`. | Facturas ya cobradas. |
| `cancelled` | `Status = 'Cancelled'`. | Facturas canceladas. |
| `draft` | `Status = 'Draft'`. | Borradores. |
| `sent` | `Status = 'Sent'`. | Enviadas (por enviar/cobrar). |

**Validación:** Si se envía un valor distinto de los anteriores, el API responde con **HTTP 400 Bad Request** y un mensaje en inglés que lista los valores válidos, por ejemplo:

- `"Invalid invoice status type: 'xyz'. Valid values: 'all', 'pending' (unpaid), 'overdue' (unpaid and past due date), 'paid', 'cancelled', 'draft', 'sent'"`

---

## 4. Parámetros con búsqueda parcial (LIKE)

Los parámetros `customerTaxId`, `customerName`, `contractNumber` e `invoiceNumber` se tratan como **búsqueda parcial** en SQL con `LIKE`:

- El valor se **recorta** (espacios al inicio y final se eliminan).
- Si después del recorte el valor no está vacío, en base de datos se usa el patrón **`%valor%`** (el texto puede aparecer en cualquier parte del campo).
- La comparación en base de datos depende del collation del servidor (normalmente **no** sensible a mayúsculas/minúsculas para estos campos).

**Ejemplos de comportamiento esperado:**

| Parámetro | Valor enviado | Condición SQL (conceptual) | Ejemplo de coincidencias |
|-----------|----------------|----------------------------|---------------------------|
| `customerTaxId` | `123` | `c.NIT LIKE '%123%'` | NIT que contenga "123". |
| `customerName` | `acme` | `c.CompanyName LIKE '%acme%'` | Razón social que contenga "acme". |
| `contractNumber` | `2024` | `ct.ContractNumber LIKE '%2024%'` | Número de contrato que contenga "2024". |
| `invoiceNumber` | `INV-` | `i.InvoiceNumber LIKE '%INV-%'` | Número de factura que contenga "INV-". |

- Si el parámetro se **omite** o queda en **blanco** después del trim, **no** se aplica ese filtro.
- Varios de estos parámetros pueden ir juntos; en ese caso se combinan con **AND** (la factura debe cumplir todos los criterios).

---

## 5. Parámetro `feeTypeId` (tipo de tarifa)

- **Tipo:** entero.
- **Valores aceptados:** `1` (monto fijo), `2` (porcentaje).
- Si se **omite** (no se envía el parámetro), se devuelven facturas de **cualquier** tipo de tarifa.
- Si se envía un valor distinto de 1 o 2, el API responde con **HTTP 400 Bad Request**, por ejemplo:
  - `"Invalid fee type ID: 3. Valid values: 1 (Fixed amount), 2 (Percentage)"`

En SQL se aplica: `ct.FeeTypeId = @FeeTypeId` (comparación exacta).

---

## 6. Ordenamiento de los resultados

El orden depende del valor efectivo de `invoiceStatusType` (después de trim y minúsculas):

| Valor efectivo de `invoiceStatusType` | Orden aplicado | Motivo |
|--------------------------------------|----------------|--------|
| `overdue` | `DueDate ASC` | Venció primero aparece primero (más urgente). |
| `paid` | `PaidDate DESC` | Últimas pagadas primero. |
| `pending` | `DueDate ASC` | Próximas a vencer primero. |
| Cualquier otro (incluido vacío, `all`, `cancelled`, `draft`, `sent`) | `InvoiceDate DESC` | Facturas más recientes primero. |

---

## 7. Estructura de la respuesta HTTP

### 7.1 Respuesta exitosa (HTTP 200 OK)

El cuerpo es un objeto JSON con la estructura estándar de la API (`ResponseStructure<T>`), donde `T` es un **array** de objetos “invoice report”. Cada elemento del array tiene los campos del reporte de facturación.

**Estructura global:**

```json
{
  "status": true,
  "statusCode": 200,
  "data": [ /* array de objetos de factura */ ],
  "message": "Found N <statusLabel> invoice(s)",
  "errorNumber": null,
  "timestamp": "2025-02-07T12:00:00"
}
```

- **`status`:** siempre `true` en éxito.
- **`statusCode`:** 200.
- **`data`:** array de objetos; cada uno representa una fila del reporte (ver siguiente sección).
- **`message`:** texto descriptivo. `<statusLabel>` es el valor de `invoiceStatusType` usado (o `"all"` si no se envió). Ejemplos:
  - `"Found 15 all invoice(s)"`
  - `"Found 3 pending invoice(s)"`
  - `"Found 0 overdue invoice(s)"`
- **`errorNumber`:** null en éxito.
- **`timestamp`:** fecha/hora de la respuesta (Costa Rica).

**Forma de cada elemento de `data` (una factura en el reporte):**

| Campo | Tipo | Descripción |
|--------|------|-------------|
| `customerId` | number | ID del cliente. |
| `taxId` | string | NIT / cédula del cliente. |
| `customerName` | string | Nombre del cliente (CompanyName). |
| `contractId` | number | ID del contrato. |
| `contractNumber` | string | Número del contrato. |
| `invoiceId` | number | ID de la factura. |
| `invoiceNumber` | string | Número de factura. |
| `invoiceDate` | string (date-time) | Fecha de la factura (ISO). |
| `dueDate` | string (date-time) | Fecha de vencimiento. |
| `subTotal` | number | Subtotal. |
| `tax` | number | Impuesto (en este contexto suele ser 0). |
| `amount` | number | Total de la factura. |
| `currencyCode` | string | Código de moneda. |
| `status` | string | Estado del documento (ej. Draft, Sent, Cancelled). |
| `paymentStatus` | string | Estado de pago (ej. Unpaid, Paid). |
| `paidDate` | string (date-time) o null | Fecha de pago; null si no está pagada. |
| `feeTypeId` | number | 1 = monto fijo, 2 = porcentaje. |
| `feeTypeName` | string | Nombre del tipo de tarifa (ej. Fixed amount, Variable Percentage). |

**Ejemplo de respuesta exitosa con dos facturas:**

```json
{
  "status": true,
  "statusCode": 200,
  "data": [
    {
      "customerId": 10,
      "taxId": "3101234567",
      "customerName": "Acme Corp",
      "contractId": 5,
      "contractNumber": "CTR-2024-001",
      "invoiceId": 101,
      "invoiceNumber": "INV-2024-001",
      "invoiceDate": "2024-01-15T00:00:00",
      "dueDate": "2024-02-15T00:00:00",
      "subTotal": 1000.00,
      "tax": 0,
      "amount": 1000.00,
      "currencyCode": "USD",
      "status": "Sent",
      "paymentStatus": "Unpaid",
      "paidDate": null,
      "feeTypeId": 1,
      "feeTypeName": "Fixed amount"
    },
    {
      "customerId": 11,
      "taxId": "3109876543",
      "customerName": "Beta S.A.",
      "contractId": 6,
      "contractNumber": "CTR-2024-002",
      "invoiceId": 102,
      "invoiceNumber": "INV-2024-002",
      "invoiceDate": "2024-01-20T00:00:00",
      "dueDate": "2024-02-20T00:00:00",
      "subTotal": 500.50,
      "tax": 0,
      "amount": 500.50,
      "currencyCode": "USD",
      "status": "Sent",
      "paymentStatus": "Unpaid",
      "paidDate": null,
      "feeTypeId": 2,
      "feeTypeName": "Variable Percentage"
    }
  ],
  "message": "Found 2 pending invoice(s)",
  "errorNumber": null,
  "timestamp": "2025-02-07T14:30:00"
}
```

**Cuando no hay facturas que cumplan el filtro:** el API devuelve igualmente **HTTP 200**, con `data` como array vacío `[]` y `message` con cantidad 0, por ejemplo: `"Found 0 overdue invoice(s)"`.

---

### 7.2 Respuesta de error por parámetros inválidos (HTTP 400 Bad Request)

Se usa cuando el valor de `invoiceStatusType` o de `feeTypeId` no es permitido. El cuerpo tiene la misma estructura de respuesta estándar, pero con `data` en null, `status` false y `statusCode` 400.

**Ejemplo (invoiceStatusType inválido):**

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Invalid invoice status type: 'invalid'. Valid values: 'all', 'pending' (unpaid), 'overdue' (unpaid and past due date), 'paid', 'cancelled', 'draft', 'sent'",
  "errorNumber": null,
  "timestamp": "2025-02-07T14:30:00"
}
```

**Ejemplo (feeTypeId inválido):**

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Invalid fee type ID: 3. Valid values: 1 (Fixed amount), 2 (Percentage)",
  "errorNumber": null,
  "timestamp": "2025-02-07T14:30:00"
}
```

---

### 7.3 Respuesta de error interno (HTTP 500 Internal Server Error)

Ante una excepción no controlada (por ejemplo fallo de base de datos), el API responde con 500. El mensaje puede incluir el mensaje de la excepción y, si existe, el mensaje de la excepción interna.

**Ejemplo:**

```json
{
  "status": false,
  "statusCode": 500,
  "data": null,
  "message": "Error retrieving invoices report: <detalle del error> | Inner: <mensaje interno si aplica>",
  "errorNumber": null,
  "timestamp": "2025-02-07T14:30:00"
}
```

---

## 8. Resumen de combinación de filtros

- **Sin parámetros:** Todas las facturas, orden por `InvoiceDate DESC`.
- **Solo `invoiceStatusType`:** Filtra por estado y aplica el orden correspondiente a ese tipo.
- **Varios parámetros:** Se aplican en conjunto con **AND** (ej.: `invoiceStatusType=pending` + `customerTaxId=123` + `feeTypeId=1` → facturas pendientes, del cliente cuyo NIT contiene "123" y de contrato tipo monto fijo).

Los parámetros de texto vacíos o solo espacios se ignoran (no se añade condición LIKE para ese parámetro).

---

## 9. Ejemplos de URLs de solicitud

- Todas las facturas:  
  `GET api/corporate/reporting/billing/invoices`

- Solo pendientes:  
  `GET api/corporate/reporting/billing/invoices?invoiceStatusType=pending`

- Solo vencidas:  
  `GET api/corporate/reporting/billing/invoices?invoiceStatusType=overdue`

- Solo pagadas:  
  `GET api/corporate/reporting/billing/invoices?invoiceStatusType=paid`

- Pendientes + cliente cuyo NIT contiene "310":  
  `GET api/corporate/reporting/billing/invoices?invoiceStatusType=pending&customerTaxId=310`

- Pendientes + nombre de cliente que contiene "Acme":  
  `GET api/corporate/reporting/billing/invoices?invoiceStatusType=pending&customerName=Acme`

- Contratos de monto fijo (1):  
  `GET api/corporate/reporting/billing/invoices?feeTypeId=1`

- Combinación: vencidas, NIT con "123", tipo porcentaje:  
  `GET api/corporate/reporting/billing/invoices?invoiceStatusType=overdue&customerTaxId=123&feeTypeId=2`

- Por número de contrato que contenga "2024":  
  `GET api/corporate/reporting/billing/invoices?contractNumber=2024`

- Por número de factura que contenga "INV-":  
  `GET api/corporate/reporting/billing/invoices?invoiceNumber=INV-`

---

## 10. Notas para la pantalla de reportería

- Usar **`invoiceStatusType`** para cambiar entre vistas: todas, pendientes, vencidas, pagadas, canceladas, borrador, enviadas.
- Los filtros por identificación y nombre del cliente (`customerTaxId`, `customerName`), contrato (`contractNumber`) y factura (`invoiceNumber`) son por **coincidencia parcial**; no hace falta el valor exacto.
- `feeTypeId` permite restringir a contratos de monto fijo (1) o porcentaje (2).
- La respuesta siempre trae **todos los campos** listados en la tabla de “Forma de cada elemento de data”; la pantalla puede mostrar u ocultar columnas según necesidad.
- Los códigos de estado HTTP y el objeto estándar (`status`, `statusCode`, `message`, `data`) permiten distinguir éxito (200 + `data`), error de validación (400) y error de servidor (500) de forma uniforme.
