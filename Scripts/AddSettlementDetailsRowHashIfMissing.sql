/*
    Script: AddSettlementDetailsRowHashIfMissing.sql
    Objetivo:
      Alinear la base de datos con el modelo: la columna RowHash en BrandPartner.SettlementDetails
      es requerida para idempotencia del bulk upload de settlement.

    Si no ejecutaste el bloque ALTER del script CreateBrandPartnerInventorySettlementTables.sql,
      verás: "Invalid column name 'RowHash'."

    Uso: ejecutar una vez contra la base VH-DB (producción / la que use la API).
*/

SET NOCOUNT ON;

IF COL_LENGTH('BrandPartner.SettlementDetails', 'RowHash') IS NULL
BEGIN
    ALTER TABLE BrandPartner.SettlementDetails ADD RowHash NVARCHAR(50) NULL;
    PRINT 'Columna RowHash agregada a BrandPartner.SettlementDetails.';
END
ELSE
BEGIN
    PRINT 'La columna RowHash ya existe en BrandPartner.SettlementDetails.';
END;
GO

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
    PRINT 'Indice IX_SettlementDetails_SettlementHeaderId_RowHash creado.';
END
ELSE
BEGIN
    PRINT 'El indice IX_SettlementDetails_SettlementHeaderId_RowHash ya existe.';
END;
GO
