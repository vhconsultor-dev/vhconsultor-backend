-- =============================================
-- Script: Creación de tablas AmazonAccountAsin y catálogos en Corporate
-- Esquema: Corporate
-- Tablas: Categories, SubCategories, ProductGroups, ReplenishmentCategories, AmazonAccountAsins
-- Índices: UK y no clúster para búsquedas
-- =============================================
-- Fecha/hora: todas en UTC-6 (hora Costa Rica). DEFAULT DATEADD(hour, -6, GETUTCDATE()).
--
-- Volumen esperado: ~30.000 registros/año (múltiples clientes/AmazonAccounts).
-- Objetivo: buenas búsquedas sin exceso de índices (cada índice ralentiza INSERT/UPDATE).
--
-- UK (Unique Keys) — obligatorios:
--   Categories:              CategoryCode
--   SubCategories:          (CategoryId, SubCategoryCode)
--   ProductGroups:           ProductGroupCode
--   ReplenishmentCategories: ReplenishmentCategoryCode
--   AmazonAccountAsins:      (AmazonAccountId, Asin)  -- un ASIN no se repite por cuenta
--
-- Estrategia de índices AmazonAccountAsins (para ~30K filas):
--   1. UK (AmazonAccountId, Asin) ya sirve para: "ASIN de esta cuenta" y "listar por cuenta".
--   2. No duplicar índice solo en AmazonAccountId (redundante con el UK).
--   3. Índice covering para lista por cuenta + orden por ModifiedDate:
--      (AmazonAccountId, ModifiedDate DESC) INCLUDE (Asin, ProductTitle, CategoryId)
--      -> evita key lookups en la pantalla "ASINs de esta cuenta".
--   4. Índices por Asin, ParentAsin, CategoryId, etc. para filtros y búsquedas (con WHERE
--      col IS NOT NULL donde aplique para ahorrar espacio y escrituras).
--   5. Upc, Ean, Isbn con filtro IS NOT NULL: búsqueda por código sin indexar NULLs.
--
-- =============================================

-- Asegurar que el schema Corporate existe (normalmente ya existe en el proyecto)
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Corporate')
BEGIN
    EXEC('CREATE SCHEMA Corporate');
END
GO

-- =============================================
-- 1. Categories (catálogo)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'Corporate' AND t.name = 'Categories')
BEGIN
    CREATE TABLE [Corporate].[Categories] (
        [CategoryId]     INT IDENTITY(1,1) NOT NULL,
        [CategoryCode]   NVARCHAR(50)  NOT NULL,
        [CategoryName]   NVARCHAR(200) NOT NULL,
        [IsActive]       BIT NOT NULL DEFAULT 1,
        [CreatedDate]    DATETIME2 NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),

        CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([CategoryId] ASC),
        CONSTRAINT [UK_Categories_CategoryCode] UNIQUE NONCLUSTERED ([CategoryCode])
    );

    CREATE NONCLUSTERED INDEX [IX_Categories_IsActive]
        ON [Corporate].[Categories] ([IsActive])
        WHERE [IsActive] = 1;

    PRINT 'Tabla [Corporate].[Categories] creada.';
END
GO

-- =============================================
-- 2. SubCategories (catálogo, depende de Categories)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'Corporate' AND t.name = 'SubCategories')
BEGIN
    CREATE TABLE [Corporate].[SubCategories] (
        [SubCategoryId]   INT IDENTITY(1,1) NOT NULL,
        [CategoryId]      INT NOT NULL,
        [SubCategoryCode] NVARCHAR(50)  NOT NULL,
        [SubCategoryName] NVARCHAR(200) NOT NULL,
        [IsActive]       BIT NOT NULL DEFAULT 1,
        [CreatedDate]    DATETIME2 NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),

        CONSTRAINT [PK_SubCategories] PRIMARY KEY CLUSTERED ([SubCategoryId] ASC),
        CONSTRAINT [FK_SubCategories_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Corporate].[Categories] ([CategoryId]),
        CONSTRAINT [UK_SubCategories_CategoryId_SubCategoryCode] UNIQUE NONCLUSTERED ([CategoryId], [SubCategoryCode])
    );

    CREATE NONCLUSTERED INDEX [IX_SubCategories_CategoryId]
        ON [Corporate].[SubCategories] ([CategoryId]);

    CREATE NONCLUSTERED INDEX [IX_SubCategories_IsActive]
        ON [Corporate].[SubCategories] ([IsActive])
        WHERE [IsActive] = 1;

    PRINT 'Tabla [Corporate].[SubCategories] creada.';
END
GO

-- =============================================
-- 3. ProductGroups (catálogo)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'Corporate' AND t.name = 'ProductGroups')
BEGIN
    CREATE TABLE [Corporate].[ProductGroups] (
        [ProductGroupId]   INT IDENTITY(1,1) NOT NULL,
        [ProductGroupCode] NVARCHAR(50)  NOT NULL,
        [ProductGroupName] NVARCHAR(200) NOT NULL,
        [IsActive]        BIT NOT NULL DEFAULT 1,
        [CreatedDate]     DATETIME2 NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),

        CONSTRAINT [PK_ProductGroups] PRIMARY KEY CLUSTERED ([ProductGroupId] ASC),
        CONSTRAINT [UK_ProductGroups_ProductGroupCode] UNIQUE NONCLUSTERED ([ProductGroupCode])
    );

    CREATE NONCLUSTERED INDEX [IX_ProductGroups_IsActive]
        ON [Corporate].[ProductGroups] ([IsActive])
        WHERE [IsActive] = 1;

    PRINT 'Tabla [Corporate].[ProductGroups] creada.';
END
GO

-- =============================================
-- 4. ReplenishmentCategories (catálogo)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'Corporate' AND t.name = 'ReplenishmentCategories')
BEGIN
    CREATE TABLE [Corporate].[ReplenishmentCategories] (
        [ReplenishmentCategoryId]   INT IDENTITY(1,1) NOT NULL,
        [ReplenishmentCategoryCode] NVARCHAR(50)  NOT NULL,
        [ReplenishmentCategoryName] NVARCHAR(200) NOT NULL,
        [IsActive]                 BIT NOT NULL DEFAULT 1,
        [CreatedDate]              DATETIME2 NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),

        CONSTRAINT [PK_ReplenishmentCategories] PRIMARY KEY CLUSTERED ([ReplenishmentCategoryId] ASC),
        CONSTRAINT [UK_ReplenishmentCategories_Code] UNIQUE NONCLUSTERED ([ReplenishmentCategoryCode])
    );

    CREATE NONCLUSTERED INDEX [IX_ReplenishmentCategories_IsActive]
        ON [Corporate].[ReplenishmentCategories] ([IsActive])
        WHERE [IsActive] = 1;

    PRINT 'Tabla [Corporate].[ReplenishmentCategories] creada.';
END
GO

-- =============================================
-- 5. AmazonAccountAsins (tabla principal)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'Corporate' AND t.name = 'AmazonAccountAsins')
BEGIN
    CREATE TABLE [Corporate].[AmazonAccountAsins] (
        [AmazonAccountAsinId]      INT IDENTITY(1,1) NOT NULL,
        [AmazonAccountId]          INT NOT NULL,
        [Asin]                     NVARCHAR(10)  NOT NULL,
        [ProductTitle]             NVARCHAR(500) NULL,
        [ManufacturerCode]        NVARCHAR(40)  NULL,
        [ParentAsin]               NVARCHAR(10)  NULL,
        [Upc]                      NVARCHAR(20)  NULL,
        [Ean]                      NVARCHAR(20)  NULL,
        [Isbn]                     NVARCHAR(20)  NULL,
        [ModelNumber]              NVARCHAR(100) NULL,
        [CategoryId]               INT NULL,
        [SubCategoryId]            INT NULL,
        [ProductGroupId]           INT NULL,
        [ReleaseDate]              DATETIME2 NULL,
        [ReplenishmentCategoryId]  INT NULL,
        [PrepInstructionsRequired]   NVARCHAR(200) NULL,
        [PrepInstructionsVendorState] NVARCHAR(200) NULL,
        [CreatedDate]              DATETIME2 NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),
        [CreatedBy]                NVARCHAR(60)  NULL,
        [ModifiedDate]             DATETIME2 NULL,
        [ModifiedBy]               NVARCHAR(60)  NULL,

        CONSTRAINT [PK_AmazonAccountAsins] PRIMARY KEY CLUSTERED ([AmazonAccountAsinId] ASC),
        CONSTRAINT [FK_AmazonAccountAsins_AmazonAccounts] FOREIGN KEY ([AmazonAccountId]) REFERENCES [Corporate].[AmazonAccounts] ([AmazonAccountId]),
        CONSTRAINT [FK_AmazonAccountAsins_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Corporate].[Categories] ([CategoryId]),
        CONSTRAINT [FK_AmazonAccountAsins_SubCategories] FOREIGN KEY ([SubCategoryId]) REFERENCES [Corporate].[SubCategories] ([SubCategoryId]),
        CONSTRAINT [FK_AmazonAccountAsins_ProductGroups] FOREIGN KEY ([ProductGroupId]) REFERENCES [Corporate].[ProductGroups] ([ProductGroupId]),
        CONSTRAINT [FK_AmazonAccountAsins_ReplenishmentCategories] FOREIGN KEY ([ReplenishmentCategoryId]) REFERENCES [Corporate].[ReplenishmentCategories] ([ReplenishmentCategoryId]),
        CONSTRAINT [UK_AmazonAccountAsins_AmazonAccountId_Asin] UNIQUE NONCLUSTERED ([AmazonAccountId], [Asin])
    );

    -- Índice covering: lista de ASINs por cuenta ordenada por ModifiedDate (evita key lookups en la UI)
    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_AmazonAccountId_ModifiedDate]
        ON [Corporate].[AmazonAccountAsins] ([AmazonAccountId], [ModifiedDate] DESC)
        INCLUDE ([Asin], [ProductTitle], [CategoryId]);

    -- Búsqueda por ASIN en todas las cuentas (ej. "¿qué cuentas tienen este ASIN?")
    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_Asin]
        ON [Corporate].[AmazonAccountAsins] ([Asin]);

    -- Filtrar por variaciones / ASIN padre
    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_ParentAsin]
        ON [Corporate].[AmazonAccountAsins] ([ParentAsin])
        WHERE [ParentAsin] IS NOT NULL;

    -- Filtros por catálogo (reportes, listados filtrados)
    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_CategoryId]
        ON [Corporate].[AmazonAccountAsins] ([CategoryId])
        WHERE [CategoryId] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_SubCategoryId]
        ON [Corporate].[AmazonAccountAsins] ([SubCategoryId])
        WHERE [SubCategoryId] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_ProductGroupId]
        ON [Corporate].[AmazonAccountAsins] ([ProductGroupId])
        WHERE [ProductGroupId] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_ReplenishmentCategoryId]
        ON [Corporate].[AmazonAccountAsins] ([ReplenishmentCategoryId])
        WHERE [ReplenishmentCategoryId] IS NOT NULL;

    -- Reportes por fecha (altas, cambios recientes)
    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_CreatedDate]
        ON [Corporate].[AmazonAccountAsins] ([CreatedDate] DESC);

    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_ModifiedDate]
        ON [Corporate].[AmazonAccountAsins] ([ModifiedDate] DESC)
        WHERE [ModifiedDate] IS NOT NULL;

    -- Búsqueda por códigos de producto (solo filas con valor = menos filas en el índice)
    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_Upc]
        ON [Corporate].[AmazonAccountAsins] ([Upc])
        WHERE [Upc] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_Ean]
        ON [Corporate].[AmazonAccountAsins] ([Ean])
        WHERE [Ean] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_AmazonAccountAsins_Isbn]
        ON [Corporate].[AmazonAccountAsins] ([Isbn])
        WHERE [Isbn] IS NOT NULL;

    PRINT 'Tabla [Corporate].[AmazonAccountAsins] creada con índices.';
END
GO

PRINT 'Script finalizado: tablas e índices Corporate (AmazonAccountAsin y catálogos) creados.';
GO
