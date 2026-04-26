-- =============================================================================
-- Script: Crear tablas de Inventario + Settlement para BrandPartner
-- Descripción:
--   - BrandPartner.InventoryItems (inventario actual por AmazonAccount + SKU)
--   - BrandPartner.SettlementHeaders (encabezado por settlement)
--   - BrandPartner.SettlementDetails (detalle por línea de settlement)
--   - Índices orientados a alto volumen de consultas (millones de registros)
-- =============================================================================

-- Crear esquema BrandPartner si no existe
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'BrandPartner')
BEGIN
    EXEC('CREATE SCHEMA BrandPartner');
END
GO

-- ============================================================================
-- Tabla: BrandPartner.InventoryItems
-- ============================================================================
IF NOT EXISTS (
    SELECT * FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'BrandPartner' AND t.name = 'InventoryItems'
)
BEGIN
    CREATE TABLE BrandPartner.InventoryItems
    (
        InventoryItemId       BIGINT         NOT NULL IDENTITY(1,1),
        AmazonAccountId       INT            NOT NULL,
        Sku                   NVARCHAR(100)  NOT NULL,
        Asin                  NVARCHAR(20)   NULL,
        ProductName           NVARCHAR(500)  NULL,
        PrepOwner             NVARCHAR(30)   NULL,
        LabelingOwner         NVARCHAR(30)   NULL,
        UnitsPerBox           DECIMAL(10,2)  NULL,
        NumberOfBoxes         INT            NULL,
        BoxLengthIn           DECIMAL(10,2)  NULL,
        BoxWidthIn            DECIMAL(10,2)  NULL,
        BoxHeightIn           DECIMAL(10,2)  NULL,
        BoxWeightLb           DECIMAL(10,2)  NULL,
        QuantityOnHand        INT            NOT NULL CONSTRAINT DF_InventoryItems_QuantityOnHand DEFAULT (0),
        CreatedAt             DATETIME2(7)   NOT NULL CONSTRAINT DF_InventoryItems_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt             DATETIME2(7)   NULL,

        CONSTRAINT PK_InventoryItems PRIMARY KEY CLUSTERED (InventoryItemId),
        CONSTRAINT FK_InventoryItems_AmazonAccounts FOREIGN KEY (AmazonAccountId)
            REFERENCES Corporate.AmazonAccounts (AmazonAccountId),
        CONSTRAINT UQ_InventoryItems_AmazonAccountId_Sku UNIQUE (AmazonAccountId, Sku),
        CONSTRAINT CK_InventoryItems_UnitsPerBox_NonNegative CHECK (UnitsPerBox IS NULL OR UnitsPerBox >= 0),
        CONSTRAINT CK_InventoryItems_NumberOfBoxes_NonNegative CHECK (NumberOfBoxes IS NULL OR NumberOfBoxes >= 0),
        CONSTRAINT CK_InventoryItems_BoxLengthIn_NonNegative CHECK (BoxLengthIn IS NULL OR BoxLengthIn >= 0),
        CONSTRAINT CK_InventoryItems_BoxWidthIn_NonNegative CHECK (BoxWidthIn IS NULL OR BoxWidthIn >= 0),
        CONSTRAINT CK_InventoryItems_BoxHeightIn_NonNegative CHECK (BoxHeightIn IS NULL OR BoxHeightIn >= 0),
        CONSTRAINT CK_InventoryItems_BoxWeightLb_NonNegative CHECK (BoxWeightLb IS NULL OR BoxWeightLb >= 0)
    );

    PRINT 'Tabla BrandPartner.InventoryItems creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.InventoryItems ya existe.';
END
GO

-- Compatibilidad para ambientes donde la tabla ya existía sin estas columnas.
IF COL_LENGTH('BrandPartner.InventoryItems', 'PrepOwner') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD PrepOwner NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'LabelingOwner') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD LabelingOwner NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'UnitsPerBox') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD UnitsPerBox DECIMAL(10,2) NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'NumberOfBoxes') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD NumberOfBoxes INT NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'BoxLengthIn') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD BoxLengthIn DECIMAL(10,2) NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'BoxWidthIn') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD BoxWidthIn DECIMAL(10,2) NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'BoxHeightIn') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD BoxHeightIn DECIMAL(10,2) NULL;
END
GO

IF COL_LENGTH('BrandPartner.InventoryItems', 'BoxWeightLb') IS NULL
BEGIN
    ALTER TABLE BrandPartner.InventoryItems ADD BoxWeightLb DECIMAL(10,2) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_InventoryItems_UnitsPerBox_NonNegative'
      AND parent_object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    ALTER TABLE BrandPartner.InventoryItems
        ADD CONSTRAINT CK_InventoryItems_UnitsPerBox_NonNegative
            CHECK (UnitsPerBox IS NULL OR UnitsPerBox >= 0);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_InventoryItems_NumberOfBoxes_NonNegative'
      AND parent_object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    ALTER TABLE BrandPartner.InventoryItems
        ADD CONSTRAINT CK_InventoryItems_NumberOfBoxes_NonNegative
            CHECK (NumberOfBoxes IS NULL OR NumberOfBoxes >= 0);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_InventoryItems_BoxLengthIn_NonNegative'
      AND parent_object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    ALTER TABLE BrandPartner.InventoryItems
        ADD CONSTRAINT CK_InventoryItems_BoxLengthIn_NonNegative
            CHECK (BoxLengthIn IS NULL OR BoxLengthIn >= 0);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_InventoryItems_BoxWidthIn_NonNegative'
      AND parent_object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    ALTER TABLE BrandPartner.InventoryItems
        ADD CONSTRAINT CK_InventoryItems_BoxWidthIn_NonNegative
            CHECK (BoxWidthIn IS NULL OR BoxWidthIn >= 0);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_InventoryItems_BoxHeightIn_NonNegative'
      AND parent_object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    ALTER TABLE BrandPartner.InventoryItems
        ADD CONSTRAINT CK_InventoryItems_BoxHeightIn_NonNegative
            CHECK (BoxHeightIn IS NULL OR BoxHeightIn >= 0);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_InventoryItems_BoxWeightLb_NonNegative'
      AND parent_object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    ALTER TABLE BrandPartner.InventoryItems
        ADD CONSTRAINT CK_InventoryItems_BoxWeightLb_NonNegative
            CHECK (BoxWeightLb IS NULL OR BoxWeightLb >= 0);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryItems_AmazonAccountId_UpdatedAt'
      AND object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryItems_AmazonAccountId_UpdatedAt
        ON BrandPartner.InventoryItems (AmazonAccountId, UpdatedAt DESC)
        INCLUDE (Sku, Asin, ProductName, PrepOwner, LabelingOwner, QuantityOnHand, NumberOfBoxes);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryItems_AmazonAccountId_Asin'
      AND object_id = OBJECT_ID('BrandPartner.InventoryItems')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryItems_AmazonAccountId_Asin
        ON BrandPartner.InventoryItems (AmazonAccountId, Asin)
        INCLUDE (Sku, QuantityOnHand)
        WHERE Asin IS NOT NULL;
END
GO

-- ============================================================================
-- Tabla: BrandPartner.SettlementHeaders
-- ============================================================================
IF NOT EXISTS (
    SELECT * FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'BrandPartner' AND t.name = 'SettlementHeaders'
)
BEGIN
    CREATE TABLE BrandPartner.SettlementHeaders
    (
        SettlementHeaderId    BIGINT          NOT NULL IDENTITY(1,1),
        AmazonAccountId       INT             NOT NULL,
        SettlementId          NVARCHAR(50)    NOT NULL,
        SettlementStartDate   DATETIME2(7)    NULL,
        SettlementEndDate     DATETIME2(7)    NULL,
        DepositDate           DATETIME2(7)    NULL,
        TotalAmount           DECIMAL(18,2)   NULL,
        Currency              NVARCHAR(10)    NULL,
        SourceFileName        NVARCHAR(500)   NULL,
        ImportedAt            DATETIME2(7)    NOT NULL CONSTRAINT DF_SettlementHeaders_ImportedAt DEFAULT (GETUTCDATE()),
        CreatedAt             DATETIME2(7)    NOT NULL CONSTRAINT DF_SettlementHeaders_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_SettlementHeaders PRIMARY KEY CLUSTERED (SettlementHeaderId),
        CONSTRAINT FK_SettlementHeaders_AmazonAccounts FOREIGN KEY (AmazonAccountId)
            REFERENCES Corporate.AmazonAccounts (AmazonAccountId),
        CONSTRAINT UQ_SettlementHeaders_AmazonAccountId_SettlementId UNIQUE (AmazonAccountId, SettlementId)
    );

    PRINT 'Tabla BrandPartner.SettlementHeaders creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.SettlementHeaders ya existe.';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementHeaders_AmazonAccountId_DepositDate'
      AND object_id = OBJECT_ID('BrandPartner.SettlementHeaders')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementHeaders_AmazonAccountId_DepositDate
        ON BrandPartner.SettlementHeaders (AmazonAccountId, DepositDate DESC)
        INCLUDE (SettlementId, SettlementStartDate, SettlementEndDate, TotalAmount, Currency, ImportedAt);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementHeaders_AmazonAccountId_SettlementEndDate'
      AND object_id = OBJECT_ID('BrandPartner.SettlementHeaders')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementHeaders_AmazonAccountId_SettlementEndDate
        ON BrandPartner.SettlementHeaders (AmazonAccountId, SettlementEndDate DESC)
        INCLUDE (SettlementId, SettlementStartDate, DepositDate, TotalAmount, Currency, ImportedAt);
END
GO

-- ============================================================================
-- Tabla: BrandPartner.SettlementDetails
-- ============================================================================
IF NOT EXISTS (
    SELECT * FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'BrandPartner' AND t.name = 'SettlementDetails'
)
BEGIN
    CREATE TABLE BrandPartner.SettlementDetails
    (
        SettlementDetailId            BIGINT           NOT NULL IDENTITY(1,1),
        SettlementHeaderId            BIGINT           NOT NULL,
        RowNumber                     INT              NULL,
        TransactionType               NVARCHAR(100)    NULL,
        OrderId                       NVARCHAR(50)     NULL,
        MerchantOrderId               NVARCHAR(50)     NULL,
        AdjustmentId                  NVARCHAR(100)    NULL,
        ShipmentId                    NVARCHAR(100)    NULL,
        MarketplaceName               NVARCHAR(100)    NULL,
        ShipmentFeeType               NVARCHAR(100)    NULL,
        ShipmentFeeAmount             DECIMAL(18,2)    NULL,
        OrderFeeType                  NVARCHAR(100)    NULL,
        OrderFeeAmount                DECIMAL(18,2)    NULL,
        FulfillmentId                 NVARCHAR(100)    NULL,
        PostedDate                    DATETIME2(7)     NULL,
        OrderItemCode                 NVARCHAR(100)    NULL,
        MerchantOrderItemId           NVARCHAR(100)    NULL,
        MerchantAdjustmentItemId      NVARCHAR(100)    NULL,
        Sku                           NVARCHAR(100)    NULL,
        QuantityPurchased             DECIMAL(18,4)    NULL,
        PriceType                     NVARCHAR(100)    NULL,
        PriceAmount                   DECIMAL(18,2)    NULL,
        ItemRelatedFeeType            NVARCHAR(150)    NULL,
        ItemRelatedFeeAmount          DECIMAL(18,2)    NULL,
        MiscFeeAmount                 DECIMAL(18,2)    NULL,
        OtherFeeAmount                DECIMAL(18,2)    NULL,
        OtherFeeReasonDescription     NVARCHAR(255)    NULL,
        PromotionId                   NVARCHAR(100)    NULL,
        PromotionType                 NVARCHAR(100)    NULL,
        PromotionAmount               DECIMAL(18,2)    NULL,
        DirectPaymentType             NVARCHAR(100)    NULL,
        DirectPaymentAmount           DECIMAL(18,2)    NULL,
        OtherAmount                   DECIMAL(18,2)    NULL,
        AffectsInventory              BIT              NOT NULL CONSTRAINT DF_SettlementDetails_AffectsInventory DEFAULT (0),
        InventoryDelta                INT              NULL,
        RawRowJson                    NVARCHAR(MAX)    NULL,
        CreatedAt                     DATETIME2(7)     NOT NULL CONSTRAINT DF_SettlementDetails_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_SettlementDetails PRIMARY KEY CLUSTERED (SettlementDetailId),
        CONSTRAINT FK_SettlementDetails_SettlementHeaders FOREIGN KEY (SettlementHeaderId)
            REFERENCES BrandPartner.SettlementHeaders (SettlementHeaderId)
    );

    PRINT 'Tabla BrandPartner.SettlementDetails creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.SettlementDetails ya existe.';
END
GO

-- Índices estratégicos para volumen alto (SettlementDetails ~ millones de filas)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_SettlementHeaderId'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_SettlementHeaderId
        ON BrandPartner.SettlementDetails (SettlementHeaderId)
        INCLUDE (PostedDate, TransactionType, Sku, QuantityPurchased, PriceAmount, ItemRelatedFeeAmount, OtherAmount, CreatedAt);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_SettlementHeaderId_PostedDate'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_SettlementHeaderId_PostedDate
        ON BrandPartner.SettlementDetails (SettlementHeaderId, PostedDate DESC)
        INCLUDE (TransactionType, OrderId, Sku, QuantityPurchased, PriceType, PriceAmount, ItemRelatedFeeType, ItemRelatedFeeAmount);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_SettlementHeaderId_Sku_PostedDate'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_SettlementHeaderId_Sku_PostedDate
        ON BrandPartner.SettlementDetails (SettlementHeaderId, Sku, PostedDate DESC)
        INCLUDE (TransactionType, QuantityPurchased, AffectsInventory, InventoryDelta, PriceType, PriceAmount)
        WHERE Sku IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_OrderId'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_OrderId
        ON BrandPartner.SettlementDetails (OrderId)
        INCLUDE (SettlementHeaderId, PostedDate, TransactionType, Sku, QuantityPurchased, PriceAmount, ItemRelatedFeeAmount)
        WHERE OrderId IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_TransactionType_PostedDate'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_TransactionType_PostedDate
        ON BrandPartner.SettlementDetails (TransactionType, PostedDate DESC)
        INCLUDE (SettlementHeaderId, OrderId, Sku, QuantityPurchased, PriceAmount, ItemRelatedFeeAmount, OtherAmount);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_AdjustmentId'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_AdjustmentId
        ON BrandPartner.SettlementDetails (AdjustmentId)
        INCLUDE (SettlementHeaderId, PostedDate, TransactionType, OtherFeeReasonDescription, OtherAmount)
        WHERE AdjustmentId IS NOT NULL;
END
GO

-- ============================================================================
-- Tabla: BrandPartner.InventorySnapshots
-- Descripción:
--   Foto de inventario por corte (settlement) para consulta histórica exacta.
--   Guarda cantidad antes y después de aplicar el settlement.
-- ============================================================================
IF NOT EXISTS (
    SELECT * FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'BrandPartner' AND t.name = 'InventorySnapshots'
)
BEGIN
    CREATE TABLE BrandPartner.InventorySnapshots
    (
        InventorySnapshotId       BIGINT         NOT NULL IDENTITY(1,1),
        SettlementHeaderId        BIGINT         NOT NULL,
        InventoryItemId           BIGINT         NOT NULL,
        Sku                       NVARCHAR(100)  NOT NULL,
        QuantityBeforeSettlement  INT            NOT NULL,
        QuantityDeltaSettlement   INT            NOT NULL,
        QuantityAfterSettlement   INT            NOT NULL,
        SnapshotDate              DATETIME2(7)   NOT NULL CONSTRAINT DF_InventorySnapshots_SnapshotDate DEFAULT (GETUTCDATE()),
        CreatedAt                 DATETIME2(7)   NOT NULL CONSTRAINT DF_InventorySnapshots_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_InventorySnapshots PRIMARY KEY CLUSTERED (InventorySnapshotId),
        CONSTRAINT FK_InventorySnapshots_SettlementHeaders FOREIGN KEY (SettlementHeaderId)
            REFERENCES BrandPartner.SettlementHeaders (SettlementHeaderId),
        CONSTRAINT FK_InventorySnapshots_InventoryItems FOREIGN KEY (InventoryItemId)
            REFERENCES BrandPartner.InventoryItems (InventoryItemId),
        CONSTRAINT UQ_InventorySnapshots_SettlementHeaderId_InventoryItemId UNIQUE (SettlementHeaderId, InventoryItemId)
    );

    PRINT 'Tabla BrandPartner.InventorySnapshots creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.InventorySnapshots ya existe.';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventorySnapshots_SettlementHeaderId'
      AND object_id = OBJECT_ID('BrandPartner.InventorySnapshots')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventorySnapshots_SettlementHeaderId
        ON BrandPartner.InventorySnapshots (SettlementHeaderId)
        INCLUDE (InventoryItemId, Sku, QuantityBeforeSettlement, QuantityDeltaSettlement, QuantityAfterSettlement, SnapshotDate);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventorySnapshots_InventoryItemId_SnapshotDate'
      AND object_id = OBJECT_ID('BrandPartner.InventorySnapshots')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventorySnapshots_InventoryItemId_SnapshotDate
        ON BrandPartner.InventorySnapshots (InventoryItemId, SnapshotDate DESC)
        INCLUDE (SettlementHeaderId, Sku, QuantityBeforeSettlement, QuantityDeltaSettlement, QuantityAfterSettlement);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventorySnapshots_Sku_SnapshotDate'
      AND object_id = OBJECT_ID('BrandPartner.InventorySnapshots')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventorySnapshots_Sku_SnapshotDate
        ON BrandPartner.InventorySnapshots (Sku, SnapshotDate DESC)
        INCLUDE (SettlementHeaderId, InventoryItemId, QuantityBeforeSettlement, QuantityDeltaSettlement, QuantityAfterSettlement);
END
GO

-- ============================================================================
-- Tabla: BrandPartner.InventoryMovements
-- Descripción:
--   Bitácora de movimientos de inventario para auditoría completa.
--   Registra movimientos automáticos por settlement y ajustes manuales.
-- ============================================================================
IF NOT EXISTS (
    SELECT * FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'BrandPartner' AND t.name = 'InventoryMovements'
)
BEGIN
    CREATE TABLE BrandPartner.InventoryMovements
    (
        InventoryMovementId     BIGINT          NOT NULL IDENTITY(1,1),
        InventoryItemId         BIGINT          NOT NULL,
        SettlementHeaderId      BIGINT          NULL,
        SettlementDetailId      BIGINT          NULL,
        MovementType            NVARCHAR(30)    NOT NULL, -- SETTLEMENT | MANUAL | INITIAL_LOAD | ADJUSTMENT
        ReasonCode              NVARCHAR(50)    NULL,     -- ORDER | REFUND | COUNT_CORRECTION | DAMAGE | etc.
        QuantityBefore          INT             NOT NULL,
        QuantityDelta           INT             NOT NULL,
        QuantityAfter           INT             NOT NULL,
        ReferenceType           NVARCHAR(30)    NULL,     -- SettlementDetail | Manual | ExternalSync | etc.
        ReferenceId             NVARCHAR(100)   NULL,
        Comments                NVARCHAR(1000)  NULL,
        CreatedBy               NVARCHAR(255)   NULL,
        CreatedAt               DATETIME2(7)    NOT NULL CONSTRAINT DF_InventoryMovements_CreatedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_InventoryMovements PRIMARY KEY CLUSTERED (InventoryMovementId),
        CONSTRAINT FK_InventoryMovements_InventoryItems FOREIGN KEY (InventoryItemId)
            REFERENCES BrandPartner.InventoryItems (InventoryItemId),
        CONSTRAINT FK_InventoryMovements_SettlementHeaders FOREIGN KEY (SettlementHeaderId)
            REFERENCES BrandPartner.SettlementHeaders (SettlementHeaderId),
        CONSTRAINT FK_InventoryMovements_SettlementDetails FOREIGN KEY (SettlementDetailId)
            REFERENCES BrandPartner.SettlementDetails (SettlementDetailId),
        CONSTRAINT CK_InventoryMovements_QuantityAfter
            CHECK (QuantityAfter = QuantityBefore + QuantityDelta),
        CONSTRAINT CK_InventoryMovements_MovementType
            CHECK (MovementType IN ('SETTLEMENT', 'MANUAL', 'INITIAL_LOAD', 'ADJUSTMENT'))
    );

    PRINT 'Tabla BrandPartner.InventoryMovements creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.InventoryMovements ya existe.';
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryMovements_InventoryItemId_CreatedAt'
      AND object_id = OBJECT_ID('BrandPartner.InventoryMovements')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryMovements_InventoryItemId_CreatedAt
        ON BrandPartner.InventoryMovements (InventoryItemId, CreatedAt DESC)
        INCLUDE (MovementType, ReasonCode, QuantityBefore, QuantityDelta, QuantityAfter, CreatedBy, SettlementHeaderId, SettlementDetailId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryMovements_SettlementDetailId'
      AND object_id = OBJECT_ID('BrandPartner.InventoryMovements')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryMovements_SettlementDetailId
        ON BrandPartner.InventoryMovements (SettlementDetailId)
        INCLUDE (InventoryItemId, SettlementHeaderId, MovementType, QuantityBefore, QuantityDelta, QuantityAfter, CreatedAt)
        WHERE SettlementDetailId IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryMovements_SettlementHeaderId_CreatedAt'
      AND object_id = OBJECT_ID('BrandPartner.InventoryMovements')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryMovements_SettlementHeaderId_CreatedAt
        ON BrandPartner.InventoryMovements (SettlementHeaderId, CreatedAt DESC)
        INCLUDE (InventoryItemId, SettlementDetailId, MovementType, ReasonCode, QuantityDelta, QuantityAfter)
        WHERE SettlementHeaderId IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryMovements_MovementType_CreatedAt'
      AND object_id = OBJECT_ID('BrandPartner.InventoryMovements')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryMovements_MovementType_CreatedAt
        ON BrandPartner.InventoryMovements (MovementType, CreatedAt DESC)
        INCLUDE (InventoryItemId, ReasonCode, QuantityDelta, CreatedBy, ReferenceType, ReferenceId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_InventoryMovements_ReferenceType_ReferenceId'
      AND object_id = OBJECT_ID('BrandPartner.InventoryMovements')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InventoryMovements_ReferenceType_ReferenceId
        ON BrandPartner.InventoryMovements (ReferenceType, ReferenceId)
        INCLUDE (InventoryItemId, MovementType, QuantityDelta, CreatedAt)
        WHERE ReferenceType IS NOT NULL AND ReferenceId IS NOT NULL;
END
GO

-- =============================================================================
-- ALTER TABLE: Agregar columna RowHash a SettlementDetails para idempotencia
-- =============================================================================
IF COL_LENGTH('BrandPartner.SettlementDetails', 'RowHash') IS NULL
BEGIN
    ALTER TABLE BrandPartner.SettlementDetails ADD RowHash NVARCHAR(50) NULL;
    PRINT 'Columna RowHash agregada a BrandPartner.SettlementDetails.';
END
ELSE
BEGIN
    PRINT 'La columna RowHash ya existe en BrandPartner.SettlementDetails.';
END
GO

-- Índice para RowHash (idempotencia)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SettlementDetails_SettlementHeaderId_RowHash'
      AND object_id = OBJECT_ID('BrandPartner.SettlementDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SettlementDetails_SettlementHeaderId_RowHash
        ON BrandPartner.SettlementDetails (SettlementHeaderId, RowHash)
        WHERE RowHash IS NOT NULL;
    PRINT 'Índice IX_SettlementDetails_SettlementHeaderId_RowHash creado correctamente.';
END
ELSE
BEGIN
    PRINT 'El índice IX_SettlementDetails_SettlementHeaderId_RowHash ya existe.';
END
GO
