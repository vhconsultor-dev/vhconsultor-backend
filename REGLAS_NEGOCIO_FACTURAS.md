# Reglas de Negocio: Generación Automática de Facturas desde Contratos

## Resumen Ejecutivo

Cuando se crea o activa un contrato, el sistema debe generar automáticamente todas las facturas según el **monto** y **plazo** del contrato. Las facturas se crean en estado `Draft` y pueden ser enviadas al cliente posteriormente.

---

## 1. Cuándo Generar Facturas

### 1.1. Eventos que Disparan la Generación

Las facturas se generan automáticamente cuando:

1. **Se crea un nuevo contrato** con `Status = 'Active'` o `Status = 'Signed'`
2. **Se activa un contrato existente** (cambio de estado a `'Active'` o `'Signed'`)
3. **Se renueva un contrato** (si `AutoRenewal = true`)

### 1.2. Validaciones Previas

Antes de generar facturas, validar que el contrato tenga:

- ✅ `StartDate` y `EndDate` definidos
- ✅ `PaymentFrequency` definido (Monthly, Quarterly, SemiAnnual, Annual)
- ✅ `PaymentDay` definido (día del mes, 1-31)
- ✅ Monto definido: `FeeAmount` O `ContractServices` con `FinalPrice`
- ✅ `CurrencyCode` definido

**Si falta alguno de estos campos, NO generar facturas y lanzar error.**

---

## 2. Cálculo del Monto de Factura

### 2.1. Fuente del Monto

El monto puede venir de **dos fuentes** (prioridad):

#### Opción A: Monto Total del Contrato (`Contracts.FeeAmount`)
- Si `Contracts.FeeAmount` está definido, usar este monto
- Dividir este monto entre el número de facturas según `PaymentFrequency`

#### Opción B: Suma de Servicios (`ContractServices.FinalPrice`)
- Si `Contracts.FeeAmount` es `NULL`, calcular la suma de `ContractServices.FinalPrice`
- Solo considerar servicios con `IsActive = 1`
- Dividir la suma entre el número de facturas según `PaymentFrequency`

**Regla:** Si ambas fuentes están disponibles, usar `FeeAmount` (tiene prioridad).

### 2.2. Cálculo del Monto por Factura

```csharp
// Ejemplo de cálculo
decimal totalAmount = contract.FeeAmount ?? contractServices.Sum(cs => cs.FinalPrice);
int numberOfInvoices = CalculateNumberOfInvoices(contract.StartDate, contract.EndDate, contract.PaymentFrequency);
decimal amountPerInvoice = totalAmount / numberOfInvoices;
```

**IMPORTANTE:** 
- El último pago puede tener un ajuste por redondeo
- Sumar todos los montos de facturas debe dar exactamente el monto total del contrato

---

## 3. Frecuencia de Facturación (`PaymentFrequency`)

### 3.1. Valores Posibles

| Valor | Descripción | Facturas por Año | Ejemplo |
|-------|-------------|------------------|---------|
| `Monthly` | Mensual | 12 | 1 factura cada mes |
| `Quarterly` | Trimestral | 4 | 1 factura cada 3 meses |
| `SemiAnnual` | Semestral | 2 | 1 factura cada 6 meses |
| `Annual` | Anual | 1 | 1 factura al año |

### 3.2. Cálculo del Número de Facturas

```csharp
int CalculateNumberOfInvoices(DateTime startDate, DateTime endDate, string paymentFrequency)
{
    int months = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month);
    
    return paymentFrequency switch
    {
        "Monthly" => months,
        "Quarterly" => months / 3,
        "SemiAnnual" => months / 6,
        "Annual" => months / 12,
        _ => throw new ArgumentException("Frecuencia de pago no válida")
    };
}
```

**Ejemplo:**
- Contrato: 1 enero 2024 - 31 diciembre 2024 (12 meses)
- `PaymentFrequency = "Monthly"` → 12 facturas
- `PaymentFrequency = "Quarterly"` → 4 facturas
- `PaymentFrequency = "Annual"` → 1 factura

---

## 4. Cálculo de Fechas de Factura

### 4.1. `InvoiceDate` (Fecha de Emisión)

- **Primera factura:** `StartDate` del contrato
- **Facturas siguientes:** Sumar el período según `PaymentFrequency` a la fecha anterior

**Ejemplo con `PaymentFrequency = "Monthly"`:**
- Factura 1: `InvoiceDate = StartDate` (ej: 2024-01-15)
- Factura 2: `InvoiceDate = StartDate + 1 mes` (ej: 2024-02-15)
- Factura 3: `InvoiceDate = StartDate + 2 meses` (ej: 2024-03-15)
- ...

### 4.2. `DueDate` (Fecha de Vencimiento)

- **Base:** `InvoiceDate + PaymentDay` (día del mes)
- **Regla:** Si `PaymentDay` es mayor que los días del mes, usar el último día del mes

**Ejemplo:**
- `PaymentDay = 15`
- `InvoiceDate = 2024-01-15`
- `DueDate = 2024-01-15` (mismo día del mes siguiente, o según política de días de gracia)

**Recomendación:** Agregar 30 días de gracia desde `InvoiceDate`:
```csharp
DateTime dueDate = invoiceDate.AddDays(30);
// Ajustar al PaymentDay si es necesario
if (contract.PaymentDay.HasValue)
{
    dueDate = new DateTime(dueDate.Year, dueDate.Month, contract.PaymentDay.Value);
}
```

---

## 5. Generación de `InvoiceNumber`

### 5.1. Formato

**Formato sugerido:** `INV-{AÑO}-{CONSECUTIVO}`

**Ejemplo:**
- `INV-2024-001`
- `INV-2024-002`
- `INV-2024-003`

### 5.2. Lógica de Generación

```csharp
public async Task<string> GenerateInvoiceNumberAsync()
{
    var year = DateTime.Now.Year;
    var lastInvoice = await _context.Invoices
        .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-"))
        .OrderByDescending(i => i.InvoiceNumber)
        .FirstOrDefaultAsync();
    
    int nextNumber = 1;
    if (lastInvoice != null)
    {
        var parts = lastInvoice.InvoiceNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
        {
            nextNumber = lastNumber + 1;
        }
    }
    
    return $"INV-{year}-{nextNumber:D3}";
}
```

**IMPORTANTE:** `InvoiceNumber` tiene constraint UNIQUE en la base de datos. Validar antes de insertar.

---

## 6. Creación de `InvoiceItems`

### 6.1. Cuándo Crear Items

**Opción A: Si el contrato tiene `FeeAmount`:**
- Crear **1 item** con:
  - `Description = contract.FeeDescription ?? "Servicios del contrato"`
  - `Quantity = 1`
  - `UnitPrice = amountPerInvoice`
  - `Discount = 0`
  - `LineTotal = amountPerInvoice`
  - `ContractServiceId = NULL`

**Opción B: Si el contrato usa `ContractServices`:**
- Crear **1 item por cada `ContractService` activo**:
  - `Description = contractService.ServiceDescription ?? service.ServiceName`
  - `Quantity = contractService.Quantity ?? 1`
  - `UnitPrice = contractService.UnitPrice`
  - `Discount = calcular descuento`
  - `LineTotal = (UnitPrice * Quantity) - Discount`
  - `ContractServiceId = contractService.ContractServiceId`
  - `ServiceOrder = contractService.ServiceOrder`

### 6.2. Cálculo de Montos en Items

```csharp
// Para cada ContractService
decimal unitPrice = contractService.UnitPrice ?? 0;
decimal quantity = contractService.Quantity ?? 1;
decimal discountPercentage = contractService.DiscountPercentage ?? 0;
decimal subtotal = unitPrice * quantity;
decimal discount = subtotal * (discountPercentage / 100);
decimal lineTotal = subtotal - discount;
```

**IMPORTANTE:** La suma de `LineTotal` de todos los items debe ser igual a `SubTotal` de la factura.

---

## 7. Estados de Factura

### 7.1. Estados Disponibles

| Estado | Descripción | Cuándo se Asigna |
|--------|-------------|------------------|
| `Draft` | Borrador | Al crear la factura automáticamente |
| `Sent` | Enviada al cliente | Cuando se envía por email/notificación |
| `Paid` | Pagada | Cuando se registra el pago |
| `Overdue` | Vencida | Cuando `DueDate < hoy` y `PaymentStatus = 'Unpaid'` |
| `Cancelled` | Cancelada | Cuando se cancela la factura |

### 7.2. `PaymentStatus`

| Estado | Descripción |
|--------|-------------|
| `Unpaid` | No pagada (default) |
| `Paid` | Pagada completamente |

**Regla:** Una factura **NO puede tener pago parcial**. Siempre se paga el total o nada.

### 7.3. Transiciones de Estado

```
Draft → Sent → Paid
  ↓       ↓
Cancelled  Overdue → Paid
```

**Reglas:**
- Solo se puede pasar de `Draft` a `Sent` manualmente (acción del usuario)
- `Overdue` se asigna automáticamente cuando `DueDate < hoy` y `PaymentStatus = 'Unpaid'`
- Al marcar como `Paid`, cambiar `Status = 'Paid'` y `PaymentStatus = 'Paid'`
- Solo se puede cancelar si `Status = 'Draft'` o `Status = 'Sent'`

---

## 8. Proceso de Pago de Factura

### 8.1. Campos Requeridos para Marcar como Pagada

Cuando se marca una factura como pagada, se debe llenar:

- ✅ `PaymentStatus = 'Paid'`
- ✅ `Status = 'Paid'`
- ✅ `PaidDate = fecha del pago`
- ✅ `PaidBy = UserId` (usuario que aplica el pago)
- ✅ `PaymentMethodId` (método de pago usado)
- ✅ `PaymentReference` (referencia de pago, opcional)
- ✅ `DepositNumber` (número de depósito, si aplica)
- ✅ `TransferNumber` (número de transferencia, si aplica)

### 8.2. Validaciones al Marcar como Pagada

1. La factura debe existir
2. `PaymentStatus` debe ser `'Unpaid'`
3. `Status` no debe ser `'Cancelled'`
4. `PaidDate` no puede ser futuro
5. `PaidBy` debe ser un usuario válido

### 8.3. Ejemplo de Código

```csharp
public async Task MarkInvoiceAsPaidAsync(int invoiceId, MarkInvoiceAsPaidDto dto, int userId)
{
    var invoice = await _context.Invoices
        .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
    
    if (invoice == null)
        throw new NotFoundException("Factura no encontrada");
    
    if (invoice.PaymentStatus == "Paid")
        throw new BusinessException("La factura ya está pagada");
    
    if (invoice.Status == "Cancelled")
        throw new BusinessException("No se puede pagar una factura cancelada");
    
    invoice.PaymentStatus = "Paid";
    invoice.Status = "Paid";
    invoice.PaidDate = dto.PaidDate ?? DateTime.Now.Date;
    invoice.PaidBy = userId;
    invoice.PaymentMethodId = dto.PaymentMethodId;
    invoice.PaymentReference = dto.PaymentReference;
    invoice.DepositNumber = dto.DepositNumber;
    invoice.TransferNumber = dto.TransferNumber;
    invoice.UpdatedAt = DateTime.Now;
    invoice.LastModifiedBy = userId.ToString();
    
    await _context.SaveChangesAsync();
}
```

---

## 9. Adjuntos de Factura (`InvoiceAttachments`)

### 9.1. Propósito

Permitir subir documentos relacionados con el pago:
- Fotos de transferencias bancarias
- Vouchers de pago
- Comprobantes de depósito
- Cualquier otro documento relacionado

### 9.2. Campos

- `InvoiceId` (FK a Invoices)
- `FileUrl` (URL del archivo almacenado)
- `UploadedBy` (FK a Users)
- `UploadedAt` (fecha/hora de carga)

### 9.3. Reglas

- Una factura puede tener **múltiples adjuntos**
- Los adjuntos se pueden subir en cualquier momento
- No hay validación de tipo de archivo en la BD (validar en backend)
- Al eliminar una factura, se eliminan sus adjuntos (CASCADE)

---

## 10. Flujo Completo de Generación

### 10.1. Algoritmo Paso a Paso

```csharp
public async Task GenerateInvoicesForContractAsync(int contractId)
{
    // 1. Obtener contrato
    var contract = await _context.Contracts
        .Include(c => c.ContractServices)
        .FirstOrDefaultAsync(c => c.ContractId == contractId);
    
    if (contract == null)
        throw new NotFoundException("Contrato no encontrado");
    
    // 2. Validar campos requeridos
    ValidateContractForInvoiceGeneration(contract);
    
    // 3. Calcular monto total
    decimal totalAmount = contract.FeeAmount ?? 
        contract.ContractServices
            .Where(cs => cs.IsActive)
            .Sum(cs => cs.FinalPrice ?? 0);
    
    // 4. Calcular número de facturas
    int numberOfInvoices = CalculateNumberOfInvoices(
        contract.StartDate.Value, 
        contract.EndDate.Value, 
        contract.PaymentFrequency
    );
    
    // 5. Calcular monto por factura
    decimal amountPerInvoice = totalAmount / numberOfInvoices;
    
    // 6. Generar facturas
    var invoices = new List<Invoice>();
    DateTime currentDate = contract.StartDate.Value;
    
    for (int i = 0; i < numberOfInvoices; i++)
    {
        // Calcular fechas
        DateTime invoiceDate = currentDate.AddMonths(i * GetMonthsForFrequency(contract.PaymentFrequency));
        DateTime dueDate = CalculateDueDate(invoiceDate, contract.PaymentDay);
        
        // Generar número de factura
        string invoiceNumber = await GenerateInvoiceNumberAsync();
        
        // Crear factura
        var invoice = new Invoice
        {
            ContractId = contractId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            SubTotal = amountPerInvoice,
            Tax = 0, // Calcular según política de impuestos
            Total = amountPerInvoice,
            CurrencyCode = contract.CurrencyCode,
            Status = "Draft",
            PaymentStatus = "Unpaid",
            CreatedAt = DateTime.Now
        };
        
        // Crear items
        await CreateInvoiceItemsAsync(invoice, contract, amountPerInvoice);
        
        invoices.Add(invoice);
    }
    
    // 7. Guardar todas las facturas
    await _context.Invoices.AddRangeAsync(invoices);
    await _context.SaveChangesAsync();
}
```

### 10.2. Funciones Auxiliares

```csharp
private void ValidateContractForInvoiceGeneration(Contract contract)
{
    if (!contract.StartDate.HasValue)
        throw new BusinessException("El contrato debe tener StartDate");
    
    if (!contract.EndDate.HasValue)
        throw new BusinessException("El contrato debe tener EndDate");
    
    if (string.IsNullOrEmpty(contract.PaymentFrequency))
        throw new BusinessException("El contrato debe tener PaymentFrequency");
    
    if (contract.FeeAmount == null && !contract.ContractServices.Any(cs => cs.IsActive))
        throw new BusinessException("El contrato debe tener FeeAmount o ContractServices activos");
    
    if (string.IsNullOrEmpty(contract.CurrencyCode))
        throw new BusinessException("El contrato debe tener CurrencyCode");
}

private int GetMonthsForFrequency(string paymentFrequency)
{
    return paymentFrequency switch
    {
        "Monthly" => 1,
        "Quarterly" => 3,
        "SemiAnnual" => 6,
        "Annual" => 12,
        _ => throw new ArgumentException("Frecuencia no válida")
    };
}

private DateTime CalculateDueDate(DateTime invoiceDate, int? paymentDay)
{
    DateTime dueDate = invoiceDate.AddDays(30); // 30 días de gracia
    
    if (paymentDay.HasValue)
    {
        int day = paymentDay.Value;
        int lastDayOfMonth = DateTime.DaysInMonth(dueDate.Year, dueDate.Month);
        day = Math.Min(day, lastDayOfMonth);
        dueDate = new DateTime(dueDate.Year, dueDate.Month, day);
    }
    
    return dueDate;
}
```

---

## 11. Ejemplos Prácticos

### Ejemplo 1: Contrato Mensual

**Contrato:**
- `StartDate = 2024-01-15`
- `EndDate = 2024-12-15`
- `PaymentFrequency = "Monthly"`
- `PaymentDay = 15`
- `FeeAmount = 12000.00`
- `CurrencyCode = "USD"`

**Resultado:**
- 12 facturas generadas
- Monto por factura: $1,000.00
- Factura 1: `InvoiceDate = 2024-01-15`, `DueDate = 2024-02-15`
- Factura 2: `InvoiceDate = 2024-02-15`, `DueDate = 2024-03-15`
- ... (hasta 12)

### Ejemplo 2: Contrato Trimestral con Servicios

**Contrato:**
- `StartDate = 2024-01-01`
- `EndDate = 2024-12-31`
- `PaymentFrequency = "Quarterly"`
- `PaymentDay = 1`
- `FeeAmount = NULL`
- `ContractServices`:
  - Servicio 1: `FinalPrice = 6000.00`
  - Servicio 2: `FinalPrice = 3000.00`
- `CurrencyCode = "USD"`

**Resultado:**
- Total: $9,000.00
- 4 facturas generadas
- Monto por factura: $2,250.00
- Cada factura tiene 2 items (uno por servicio)
- Factura 1: `InvoiceDate = 2024-01-01`, `DueDate = 2024-02-01`
- Factura 2: `InvoiceDate = 2024-04-01`, `DueDate = 2024-05-01`
- ... (hasta 4)

---

## 12. Validaciones y Reglas Adicionales

### 12.1. No Duplicar Facturas

- Antes de generar, verificar que no existan facturas para ese contrato
- Si ya existen, lanzar error o preguntar si se desea regenerar (eliminar y crear nuevas)

### 12.2. Contratos Modificados

- Si se modifica un contrato después de generar facturas, **NO regenerar automáticamente**
- Las facturas ya generadas mantienen sus montos originales
- Si se necesita regenerar, hacerlo manualmente (eliminar facturas existentes y crear nuevas)

### 12.3. Facturas Vencidas

- Crear un job/proceso que actualice automáticamente el estado a `Overdue` cuando `DueDate < hoy` y `PaymentStatus = 'Unpaid'`
- Ejecutar diariamente

### 12.4. Impuestos

- Por ahora, `Tax = 0` por defecto
- Si en el futuro se necesita calcular impuestos, agregar lógica según país/moneda

---

## 13. Endpoints Sugeridos

### 13.1. Generar Facturas

```
POST /api/contracts/{contractId}/invoices/generate
```

**Request:** (vacío o con opciones)
```json
{
  "regenerate": false  // Si true, elimina facturas existentes y crea nuevas
}
```

**Response:**
```json
{
  "contractId": 123,
  "invoicesGenerated": 12,
  "totalAmount": 12000.00,
  "invoices": [...]
}
```

### 13.2. Listar Facturas de un Contrato

```
GET /api/contracts/{contractId}/invoices
```

### 13.3. Marcar Factura como Pagada

```
PUT /api/invoices/{invoiceId}/mark-as-paid
```

**Request:**
```json
{
  "paidDate": "2024-02-15",
  "paymentMethodId": 1,
  "paymentReference": "REF-12345",
  "depositNumber": "DEP-67890",
  "transferNumber": "TRF-11111"
}
```

### 13.4. Subir Adjunto

```
POST /api/invoices/{invoiceId}/attachments
```

**Request:** (multipart/form-data)
- `file`: archivo a subir

---

## 14. Checklist de Implementación

- [ ] Validar campos requeridos del contrato antes de generar
- [ ] Calcular correctamente el número de facturas según frecuencia
- [ ] Calcular correctamente el monto por factura (con ajuste en última factura)
- [ ] Generar `InvoiceNumber` único y secuencial
- [ ] Calcular `DueDate` correctamente según `PaymentDay`
- [ ] Crear `InvoiceItems` según fuente de monto (FeeAmount o ContractServices)
- [ ] Validar que no se dupliquen facturas
- [ ] Implementar transiciones de estado correctas
- [ ] Validar campos al marcar como pagada
- [ ] Implementar carga de adjuntos
- [ ] Crear job para actualizar facturas vencidas
- [ ] Implementar endpoints sugeridos
- [ ] Agregar logging/auditoría de operaciones

---

## 15. Notas Importantes

1. **No hay pago parcial:** Una factura se paga completamente o no se paga
2. **Facturas son inmutables:** Una vez creadas, no se modifican automáticamente
3. **Regeneración manual:** Si se necesita regenerar, hacerlo manualmente eliminando y creando nuevas
4. **Snapshot del cliente:** La factura usa datos del contrato al momento de generación (no se actualiza si cambia el cliente)
5. **Moneda:** Todas las facturas de un contrato usan la misma moneda (`CurrencyCode` del contrato)

---

## Contacto

Si tienes dudas sobre la implementación, consulta con el equipo de base de datos o revisa los scripts SQL de creación de tablas.

