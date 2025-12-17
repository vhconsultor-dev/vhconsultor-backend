-- =============================================
-- Script: Creación de Tablas de Facturas (Invoices)
-- Descripción: Crea las tablas Invoices, InvoiceItems e InvoiceAttachments
-- Autor: VHConsultor Team
-- Fecha: 2024
-- =============================================

USE [VH-DB];
GO

-- =============================================
-- 1. Tabla: Invoices
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices' AND schema_id = SCHEMA_ID('Corporate'))
BEGIN
    CREATE TABLE [Corporate].[Invoices] (
        [InvoiceId] INT IDENTITY(1,1) NOT NULL,
        [ContractId] NVARCHAR(50) NOT NULL,
        [InvoiceNumber] NVARCHAR(100) NOT NULL,
        [InvoiceDate] DATETIME NOT NULL,
        [DueDate] DATETIME NOT NULL,
        [SubTotal] DECIMAL(15,2) NOT NULL,
        [Tax] DECIMAL(15,2) NOT NULL DEFAULT 0,
        [Total] DECIMAL(15,2) NOT NULL,
        [CurrencyCode] NVARCHAR(3) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Unpaid',
        [PaidDate] DATETIME NULL,
        [PaidBy] INT NULL,
        [PaymentMethodId] INT NULL,
        [PaymentReference] NVARCHAR(255) NULL,
        [DepositNumber] NVARCHAR(255) NULL,
        [TransferNumber] NVARCHAR(255) NULL,
        [Notes] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT (DATEADD(hour, -6, GETUTCDATE())),
        [UpdatedAt] DATETIME NULL,
        [LastModifiedBy] NVARCHAR(255) NULL,
        
        CONSTRAINT [PK_Invoices] PRIMARY KEY CLUSTERED ([InvoiceId] ASC),
        CONSTRAINT [UK_Invoices_InvoiceNumber] UNIQUE ([InvoiceNumber]),
        CONSTRAINT [FK_Invoices_Contracts] FOREIGN KEY ([ContractId]) 
            REFERENCES [Corporate].[Contracts]([ContractId]),
        CONSTRAINT [CK_Invoices_Status] CHECK ([Status] IN ('Draft', 'Sent', 'Paid', 'Overdue', 'Cancelled')),
        CONSTRAINT [CK_Invoices_PaymentStatus] CHECK ([PaymentStatus] IN ('Unpaid', 'Paid'))
    );
    
    -- Índices
    CREATE NONCLUSTERED INDEX [IX_Invoices_ContractId] ON [Corporate].[Invoices]([ContractId]);
    CREATE NONCLUSTERED INDEX [IX_Invoices_Status] ON [Corporate].[Invoices]([Status]);
    CREATE NONCLUSTERED INDEX [IX_Invoices_PaymentStatus] ON [Corporate].[Invoices]([PaymentStatus]);
    CREATE NONCLUSTERED INDEX [IX_Invoices_DueDate] ON [Corporate].[Invoices]([DueDate]);
    CREATE NONCLUSTERED INDEX [IX_Invoices_InvoiceDate] ON [Corporate].[Invoices]([InvoiceDate]);
    
    PRINT 'Tabla [Corporate].[Invoices] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [Corporate].[Invoices] ya existe.';
END
GO

-- =============================================
-- 2. Tabla: InvoiceItems
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceItems' AND schema_id = SCHEMA_ID('Corporate'))
BEGIN
    CREATE TABLE [Corporate].[InvoiceItems] (
        [InvoiceItemId] INT IDENTITY(1,1) NOT NULL,
        [InvoiceId] INT NOT NULL,
        [ContractServiceId] INT NULL,
        [Description] NVARCHAR(1000) NOT NULL,
        [Quantity] DECIMAL(10,2) NOT NULL DEFAULT 1,
        [UnitPrice] DECIMAL(15,2) NOT NULL,
        [Discount] DECIMAL(15,2) NOT NULL DEFAULT 0,
        [LineTotal] DECIMAL(15,2) NOT NULL,
        [ServiceOrder] INT NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT (DATEADD(hour, -6, GETUTCDATE())),
        
        CONSTRAINT [PK_InvoiceItems] PRIMARY KEY CLUSTERED ([InvoiceItemId] ASC),
        CONSTRAINT [FK_InvoiceItems_Invoices] FOREIGN KEY ([InvoiceId]) 
            REFERENCES [Corporate].[Invoices]([InvoiceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_InvoiceItems_ContractServices] FOREIGN KEY ([ContractServiceId]) 
            REFERENCES [Corporate].[ContractServices]([ContractServiceId])
    );
    
    -- Índices
    CREATE NONCLUSTERED INDEX [IX_InvoiceItems_InvoiceId] ON [Corporate].[InvoiceItems]([InvoiceId]);
    CREATE NONCLUSTERED INDEX [IX_InvoiceItems_ContractServiceId] ON [Corporate].[InvoiceItems]([ContractServiceId]);
    
    PRINT 'Tabla [Corporate].[InvoiceItems] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [Corporate].[InvoiceItems] ya existe.';
END
GO

-- =============================================
-- 3. Tabla: InvoiceAttachments
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceAttachments' AND schema_id = SCHEMA_ID('Corporate'))
BEGIN
    CREATE TABLE [Corporate].[InvoiceAttachments] (
        [InvoiceAttachmentId] INT IDENTITY(1,1) NOT NULL,
        [InvoiceId] INT NOT NULL,
        [FileUrl] NVARCHAR(500) NOT NULL,
        [FileName] NVARCHAR(255) NULL,
        [FileType] NVARCHAR(100) NULL,
        [FileSize] BIGINT NULL,
        [UploadedBy] INT NULL,
        [UploadedAt] DATETIME NOT NULL DEFAULT (DATEADD(hour, -6, GETUTCDATE())),
        
        CONSTRAINT [PK_InvoiceAttachments] PRIMARY KEY CLUSTERED ([InvoiceAttachmentId] ASC),
        CONSTRAINT [FK_InvoiceAttachments_Invoices] FOREIGN KEY ([InvoiceId]) 
            REFERENCES [Corporate].[Invoices]([InvoiceId]) ON DELETE CASCADE
    );
    
    -- Índices
    CREATE NONCLUSTERED INDEX [IX_InvoiceAttachments_InvoiceId] ON [Corporate].[InvoiceAttachments]([InvoiceId]);
    
    PRINT 'Tabla [Corporate].[InvoiceAttachments] creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla [Corporate].[InvoiceAttachments] ya existe.';
END
GO

-- =============================================
-- 4. Verificación de Tablas Creadas
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'RESUMEN DE TABLAS CREADAS:';
PRINT '========================================';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices' AND schema_id = SCHEMA_ID('Corporate'))
    PRINT '✓ [Corporate].[Invoices] - OK';
ELSE
    PRINT '✗ [Corporate].[Invoices] - ERROR';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceItems' AND schema_id = SCHEMA_ID('Corporate'))
    PRINT '✓ [Corporate].[InvoiceItems] - OK';
ELSE
    PRINT '✗ [Corporate].[InvoiceItems] - ERROR';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'InvoiceAttachments' AND schema_id = SCHEMA_ID('Corporate'))
    PRINT '✓ [Corporate].[InvoiceAttachments] - OK';
ELSE
    PRINT '✗ [Corporate].[InvoiceAttachments] - ERROR';

PRINT '========================================';
PRINT 'Script completado exitosamente.';
PRINT '========================================';
GO

