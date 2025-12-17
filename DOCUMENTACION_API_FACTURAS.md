# Documentación API de Facturas (Invoices)

## Resumen

Esta API permite la generación automática de facturas desde contratos, así como la gestión completa del ciclo de vida de las facturas.

---

## Endpoints Disponibles

### 1. Generar Facturas Automáticamente

**POST** `/api/corporate/invoice/generate/{contractId}`

Genera todas las facturas automáticamente para un contrato específico.

#### Parámetros de URL
- `contractId` (string, requerido): ID del contrato

#### Respuesta Exitosa (200 OK)
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

#### Respuesta si ya existen facturas (400 Bad Request)
```json
{
  "success": false,
  "message": "Ya existen facturas generadas para este contrato",
  "data": {
    "success": false,
    "message": "Ya existen facturas generadas para este contrato",
    "invoicesGenerated": 0,
    "existingInvoicesCount": 12,
    "totalAmount": 0,
    "invoiceIds": []
  }
}
```

#### Validaciones
- El contrato debe existir
- El contrato debe tener: `StartDate`, `EndDate`, `PaymentFrequency`, `CurrencyCode`
- El contrato debe tener `FeeAmount` o `ContractServices` activos
- No deben existir facturas previas para el contrato

---

### 2. Marcar Factura como Pagada

**PUT** `/api/corporate/invoice/{invoiceId}/mark-as-paid`

Marca una factura como pagada y registra los detalles del pago.

#### Parámetros de URL
- `invoiceId` (int, requerido): ID de la factura

#### Body (JSON)
```json
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 1,
  "paymentReference": "REF-12345",
  "depositNumber": "DEP-67890",
  "transferNumber": "TRF-11111"
}
```

#### Campos del Body
- `paidDate` (DateTime, opcional): Fecha del pago (por defecto: hoy)
- `paymentMethodId` (int, opcional): ID del método de pago
- `paymentReference` (string, opcional): Referencia del pago
- `depositNumber` (string, opcional): Número de depósito
- `transferNumber` (string, opcional): Número de transferencia

#### Respuesta Exitosa (200 OK)
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

#### Validaciones
- La factura debe existir
- La factura no debe estar ya pagada
- La factura no debe estar cancelada
- La fecha de pago no puede ser futura

---

### 3. Obtener Facturas con Filtros (Query Unificado)

**GET** `/api/corporate/invoice`

Obtiene facturas con múltiples filtros opcionales. Este es un endpoint unificado que maneja todos los escenarios de consulta.

#### Parámetros de Query (todos opcionales)

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `invoiceId` | int | ID específico de factura | `?invoiceId=1` |
| `contractId` | string | Filtrar por contrato | `?contractId=CONT-001` |
| `invoiceNumber` | string | Buscar por número (parcial) | `?invoiceNumber=INV-2024` |
| `status` | string | Filtrar por estado | `?status=Draft` |
| `paymentStatus` | string | Filtrar por estado de pago | `?paymentStatus=Unpaid` |
| `currencyCode` | string | Filtrar por moneda | `?currencyCode=USD` |
| `invoiceDateFrom` | DateTime | Fecha emisión desde | `?invoiceDateFrom=2024-01-01` |
| `invoiceDateTo` | DateTime | Fecha emisión hasta | `?invoiceDateTo=2024-12-31` |
| `dueDateFrom` | DateTime | Fecha vencimiento desde | `?dueDateFrom=2024-01-01` |
| `dueDateTo` | DateTime | Fecha vencimiento hasta | `?dueDateTo=2024-12-31` |
| `isOverdue` | bool | Solo facturas vencidas | `?isOverdue=true` |
| `orderBy` | string | Ordenamiento | `?orderBy=InvoiceDate` |
| `pageNumber` | int | Número de página | `?pageNumber=1` |
| `pageSize` | int | Tamaño de página | `?pageSize=10` |

#### Valores de `status`
- `Draft`: Borrador
- `Sent`: Enviada
- `Paid`: Pagada
- `Overdue`: Vencida
- `Cancelled`: Cancelada

#### Valores de `paymentStatus`
- `Unpaid`: No pagada
- `Paid`: Pagada

#### Valores de `orderBy`
- `InvoiceDate`: Por fecha de emisión (descendente)
- `DueDate`: Por fecha de vencimiento (ascendente)
- `Total`: Por monto total (descendente)
- `Status`: Por estado

#### Ejemplos de Uso

**Todas las facturas de un contrato:**
```
GET /api/corporate/invoice?contractId=CONT-001
```

**Facturas en borrador:**
```
GET /api/corporate/invoice?status=Draft
```

**Facturas vencidas:**
```
GET /api/corporate/invoice?isOverdue=true
```

**Facturas de un contrato con paginación:**
```
GET /api/corporate/invoice?contractId=CONT-001&pageNumber=1&pageSize=10
```

**Facturas no pagadas de enero 2024:**
```
GET /api/corporate/invoice?paymentStatus=Unpaid&invoiceDateFrom=2024-01-01&invoiceDateTo=2024-01-31
```

#### Respuesta Exitosa (200 OK)
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
      }
    ],
    "totalRecords": 12,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 2
  }
}
```

---

### 4. Obtener Detalle de Factura por ID

**GET** `/api/corporate/invoice/{invoiceId}`

Obtiene el detalle completo de una factura incluyendo sus items y adjuntos.

#### Parámetros de URL
- `invoiceId` (int, requerido): ID de la factura

#### Respuesta Exitosa (200 OK)
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
      "notes": null,
      "createdAt": "2024-01-15T10:00:00"
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

---

### 5. Obtener Resumen de Facturas por Contrato

**GET** `/api/corporate/invoice/summary/{contractId}`

Obtiene un resumen estadístico de las facturas de un contrato.

#### Parámetros de URL
- `contractId` (string, requerido): ID del contrato

#### Respuesta Exitosa (200 OK)
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

---

## Flujo de Trabajo Recomendado

### 1. Crear Contrato
1. Crear el contrato con todos los datos requeridos
2. Asegurarse de que tenga: `StartDate`, `EndDate`, `PaymentFrequency`, `PaymentDay`, `CurrencyCode`
3. Definir `FeeAmount` o agregar `ContractServices` activos

### 2. Generar Facturas
1. Llamar a `POST /api/corporate/invoice/generate/{contractId}`
2. El sistema valida el contrato
3. Si ya existen facturas, retorna un mensaje
4. Si no existen, genera todas las facturas en estado `Draft`

### 3. Consultar Facturas
1. Usar `GET /api/corporate/invoice?contractId={contractId}` para ver todas las facturas del contrato
2. Usar `GET /api/corporate/invoice/{invoiceId}` para ver el detalle de una factura específica
3. Usar `GET /api/corporate/invoice/summary/{contractId}` para ver el resumen

### 4. Marcar como Pagada
1. Cuando el cliente pague, llamar a `PUT /api/corporate/invoice/{invoiceId}/mark-as-paid`
2. Enviar los datos del pago (fecha, método, referencia, etc.)
3. La factura cambia a estado `Paid`

---

## Códigos de Error

| Código | Descripción |
|--------|-------------|
| 200 | Operación exitosa |
| 400 | Error de validación (datos incorrectos) |
| 401 | No autenticado |
| 404 | Recurso no encontrado |
| 500 | Error interno del servidor |

---

## Autenticación

Todos los endpoints requieren autenticación JWT. Incluir el token en el header:

```
Authorization: Bearer {token}
```

---

## Notas Importantes

1. **Facturas Inmutables**: Una vez generadas, las facturas no se modifican automáticamente si se cambia el contrato
2. **No Duplicar**: El sistema previene la generación duplicada de facturas para el mismo contrato
3. **Pago Completo**: No hay pagos parciales, la factura se paga completamente o no se paga
4. **Ajuste de Redondeo**: La última factura puede tener un ajuste para que la suma total sea exacta
5. **Estado Overdue**: Se debe implementar un job que actualice automáticamente las facturas vencidas

---

## Ejemplos de Integración Frontend

### Botón "Generar Facturas" en Pantalla de Contrato

```javascript
async function generarFacturas(contractId) {
  try {
    const response = await fetch(`/api/corporate/invoice/generate/${contractId}`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    const result = await response.json();
    
    if (result.success) {
      alert(`✓ ${result.message}`);
      // Recargar lista de facturas
      cargarFacturas(contractId);
    } else {
      alert(`⚠ ${result.message}`);
    }
  } catch (error) {
    alert('Error al generar facturas');
  }
}
```

### Listar Facturas de un Contrato

```javascript
async function cargarFacturas(contractId) {
  try {
    const response = await fetch(`/api/corporate/invoice?contractId=${contractId}`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });
    
    const result = await response.json();
    
    if (result.success) {
      mostrarFacturas(result.data.invoices);
    }
  } catch (error) {
    console.error('Error al cargar facturas', error);
  }
}
```

### Marcar Factura como Pagada

```javascript
async function marcarComoPagada(invoiceId, datosPago) {
  try {
    const response = await fetch(`/api/corporate/invoice/${invoiceId}/mark-as-paid`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(datosPago)
    });
    
    const result = await response.json();
    
    if (result.success) {
      alert('✓ Factura marcada como pagada');
      // Recargar factura
      cargarDetalleFactura(invoiceId);
    }
  } catch (error) {
    alert('Error al marcar factura como pagada');
  }
}
```

---

## Soporte

Para dudas o problemas, contactar al equipo de desarrollo.

