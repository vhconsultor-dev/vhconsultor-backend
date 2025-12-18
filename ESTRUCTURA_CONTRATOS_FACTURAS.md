# Estructura de Tablas: Contratos y Facturas

## 1. Tabla: `[Corporate].[Contracts]`

**Descripción:** Contratos con clientes, incluye snapshot del cliente al momento de firma

```sql
CREATE TABLE [Corporate].[Contracts] (
    [ContractId] INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT NOT NULL,
    [ContractNumber] NVARCHAR(100) NOT NULL,
    
    -- SNAPSHOT DEL CLIENTE (al momento de firmar)
    [ClientLegalName] NVARCHAR(255) NULL,
    [ClientTaxId] NVARCHAR(50) NULL,
    [ClientNationality] NVARCHAR(100) NULL,
    [ClientAddress] NVARCHAR(MAX) NULL,
    [ClientPrimaryContact] NVARCHAR(255) NULL,
    [ClientEmail] NVARCHAR(255) NULL,
    [ClientPhone] NVARCHAR(50) NULL,
    
    -- TIPO Y DESCRIPCIÓN
    [ContractTypeId] INT NULL,
    [ServiceDescription] NVARCHAR(MAX) NULL,
    
    -- TÉRMINOS FINANCIEROS
    [FeeTypeId] INT NULL,
    [FeeAmount] DECIMAL(10,4) NULL,
    [FeeDescription] NVARCHAR(MAX) NULL,
    [CurrencyCode] CHAR(3) NULL,
    
    -- PLAZO Y FRECUENCIA DE PAGO
    [ContractTerm] NVARCHAR(50) NULL,
    [PaymentFrequency] NVARCHAR(50) NULL,
    [PaymentDay] INT NULL,
    [PaymentMethodId] INT NULL,
    
    -- FECHAS
    [SignedDate] DATE NULL,
    [EffectiveDate] DATE NULL,
    [StartDate] DATE NULL,
    [EndDate] DATE NULL,
    [AutoRenewal] BIT NULL DEFAULT 0,
    [RenewalTerm] NVARCHAR(50) NULL,
    [RenewalNoticeDays] INT NULL,
    
    -- TERMINACIÓN
    [NoticePeriodDays] INT NULL,
    
    -- ESTADO
    [Status] NVARCHAR(50) NULL,
    
    -- INFORMACIÓN LEGAL
    [GoverningLaw] NVARCHAR(100) NULL,
    [DisputeResolution] NVARCHAR(100) NULL,
    [ContractualDomicile] NVARCHAR(MAX) NULL,
    [Jurisdiction] NVARCHAR(100) NULL,
    
    -- METADATA
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    [CreatedBy] NVARCHAR(255) NULL,
    [LastModifiedBy] NVARCHAR(255) NULL,
    
    -- DOCUMENTOS
    [DocumentUrl] NVARCHAR(500) NULL,
    [SignedDocumentUrl] NVARCHAR(500) NULL,
    
    CONSTRAINT [PK_Contracts] PRIMARY KEY ([ContractId])
);
```

### Constraints
- **PK:** `PK_Contracts` en `ContractId`
- **UNIQUE:** `UQ_Contracts_ContractNumber` en `ContractNumber`
- **FK:** `FK_Contracts_Customers` → `Customers(CustomerId)`
- **FK:** `FK_Contracts_ContractTypes` → `ContractTypes(ContractTypeId)`
- **FK:** `FK_Contracts_FeeTypes` → `FeeTypes(FeeTypeId)`
- **FK:** `FK_Contracts_Currencies` → `Currencies(CurrencyCode)`
- **FK:** `FK_Contracts_PaymentMethods` → `PaymentMethods(PaymentMethodId)`

---

## 2. Tabla: `[Corporate].[ContractServices]`

**Descripción:** Servicios asociados a cada contrato (relación N:N)

```sql
CREATE TABLE [Corporate].[ContractServices] (
    [ContractServiceId] INT IDENTITY(1,1) NOT NULL,
    [ContractId] INT NOT NULL,
    [ServiceId] INT NOT NULL,
    
    -- PERSONALIZACIÓN DEL SERVICIO EN ESTE CONTRATO
    [ServiceDescription] NVARCHAR(MAX) NULL,
    [Regions] NVARCHAR(MAX) NULL,
    [UnitPrice] DECIMAL(15,2) NULL,
    [Quantity] DECIMAL(10,2) NULL,
    [DiscountPercentage] DECIMAL(5,2) NULL,
    [FinalPrice] DECIMAL(15,2) NULL,
    
    -- CONFIGURACIÓN
    [IsActive] BIT NOT NULL DEFAULT 1,
    [ServiceOrder] INT NULL,
    [BillingFrequency] NVARCHAR(50) NULL,
    
    -- METADATA
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    
    CONSTRAINT [PK_ContractServices] PRIMARY KEY ([ContractServiceId])
);
```

### Constraints
- **PK:** `PK_ContractServices` en `ContractServiceId`
- **FK:** `FK_ContractServices_Contracts` → `Contracts(ContractId)` **ON DELETE CASCADE**
- **FK:** `FK_ContractServices_Services` → `Services(ServiceId)`
- **INDEX:** `IX_ContractServices_ContractId` en `ContractId`
- **INDEX:** `IX_ContractServices_ServiceId` en `ServiceId`

---

## 3. Tabla: `[Corporate].[Invoices]`

**Descripción:** Facturas generadas automáticamente desde contratos según monto y plazo

```sql
CREATE TABLE [Corporate].[Invoices] (
    [InvoiceId] INT IDENTITY(1,1) NOT NULL,
    [ContractId] INT NOT NULL,
    [InvoiceNumber] NVARCHAR(100) NOT NULL,
    
    -- FECHAS
    [InvoiceDate] DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    [DueDate] DATE NOT NULL,
    
    -- MONTOS
    [SubTotal] DECIMAL(18,2) NOT NULL,
    [Tax] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [Total] DECIMAL(18,2) NOT NULL,
    [CurrencyCode] CHAR(3) NOT NULL,
    
    -- ESTADO
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, Sent, Paid, Overdue, Cancelled
    [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Unpaid', -- Unpaid, Paid
    
    -- INFORMACIÓN DE PAGO
    [PaidDate] DATE NULL,
    [PaidBy] INT NULL, -- Usuario que aplicó el pago
    [PaymentMethodId] INT NULL,
    [PaymentReference] NVARCHAR(255) NULL,
    [DepositNumber] NVARCHAR(100) NULL,
    [TransferNumber] NVARCHAR(100) NULL,
    
    -- METADATA
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    [CreatedBy] NVARCHAR(255) NULL,
    [LastModifiedBy] NVARCHAR(255) NULL,
    
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([InvoiceId])
);
```

### Constraints
- **PK:** `PK_Invoices` en `InvoiceId`
- **UNIQUE:** `UQ_Invoices_InvoiceNumber` en `InvoiceNumber`
- **FK:** `FK_Invoices_Contracts` → `Contracts(ContractId)` **ON DELETE CASCADE**
- **FK:** `FK_Invoices_Currencies` → `Currencies(CurrencyCode)`
- **FK:** `FK_Invoices_PaymentMethods` → `PaymentMethods(PaymentMethodId)`
- **FK:** `FK_Invoices_Users_PaidBy` → `Users(UserId)`
- **INDEX:** `IX_Invoices_ContractId` en `ContractId`
- **INDEX:** `IX_Invoices_Status` en `Status`
- **INDEX:** `IX_Invoices_PaymentStatus` en `PaymentStatus`
- **INDEX:** `IX_Invoices_DueDate` en `DueDate`

---

## 4. Tabla: `[Corporate].[InvoiceItems]`

**Descripción:** Items/detalle de cada factura. Una factura puede tener múltiples items

```sql
CREATE TABLE [Corporate].[InvoiceItems] (
    [InvoiceItemId] INT IDENTITY(1,1) NOT NULL,
    [InvoiceId] INT NOT NULL,
    [ContractServiceId] INT NULL, -- Opcional: referencia al servicio del contrato
    
    -- DETALLE DEL ITEM
    [Description] NVARCHAR(MAX) NOT NULL,
    [Quantity] DECIMAL(10,2) NOT NULL DEFAULT 1,
    [UnitPrice] DECIMAL(18,2) NOT NULL,
    [Discount] DECIMAL(18,2) NOT NULL DEFAULT 0,
    [LineTotal] DECIMAL(18,2) NOT NULL,
    
    -- ORDEN
    [ServiceOrder] INT NULL,
    
    -- METADATA
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT [PK_InvoiceItems] PRIMARY KEY ([InvoiceItemId])
);
```

### Constraints
- **PK:** `PK_InvoiceItems` en `InvoiceItemId`
- **FK:** `FK_InvoiceItems_Invoices` → `Invoices(InvoiceId)` **ON DELETE CASCADE**
- **FK:** `FK_InvoiceItems_ContractServices` → `ContractServices(ContractServiceId)`
- **INDEX:** `IX_InvoiceItems_InvoiceId` en `InvoiceId`

---

## 5. Tabla: `[Corporate].[InvoiceAttachments]`

**Descripción:** Documentos adjuntos de facturas (fotos de transferencias, vouchers, etc.)

```sql
CREATE TABLE [Corporate].[InvoiceAttachments] (
    [InvoiceAttachmentId] INT IDENTITY(1,1) NOT NULL,
    [InvoiceId] INT NOT NULL,
    [FileUrl] NVARCHAR(500) NOT NULL,
    [UploadedBy] INT NOT NULL,
    [UploadedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT [PK_InvoiceAttachments] PRIMARY KEY ([InvoiceAttachmentId])
);
```

### Constraints
- **PK:** `PK_InvoiceAttachments` en `InvoiceAttachmentId`
- **FK:** `FK_InvoiceAttachments_Invoices` → `Invoices(InvoiceId)` **ON DELETE CASCADE**
- **FK:** `FK_InvoiceAttachments_Users` → `Users(UserId)`
- **INDEX:** `IX_InvoiceAttachments_InvoiceId` en `InvoiceId`

---

## Diagrama de Relaciones

```
Customers (1) ──< Contracts (N)
                      │
                      ├──< ContractServices (N)
                      │
                      └──< Invoices (N)
                              │
                              ├──< InvoiceItems (N)
                              │
                              └──< InvoiceAttachments (N)
```

---

## Notas Importantes

1. **ON DELETE CASCADE**: Al eliminar un contrato, se eliminan automáticamente:
   - `ContractServices`
   - `Invoices`
   - `InvoiceItems` (por cascada de Invoices)
   - `InvoiceAttachments` (por cascada de Invoices)

2. **Campos IDENTITY**: Los IDs son autoincrementales, no se asignan manualmente

3. **Campos UNIQUE**: 
   - `ContractNumber` debe ser único
   - `InvoiceNumber` debe ser único

4. **Monedas**: Se usa `CHAR(3)` para códigos ISO 4217 (USD, EUR, etc.)

5. **Decimales**:
   - `FeeAmount`: DECIMAL(10,4) - permite hasta 4 decimales
   - Montos de invoice: DECIMAL(18,2) - 2 decimales estándar
   - Precios de servicios: DECIMAL(15,2)

