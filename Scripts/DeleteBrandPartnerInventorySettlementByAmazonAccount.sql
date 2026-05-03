/*
    Script: DeleteBrandPartnerInventorySettlementByAmazonAccount.sql
    Objetivo:
      Eliminar todos los datos de pruebas de las tablas nuevas de BrandPartner
      relacionados a un AmazonAccountId específico.

    Tablas afectadas:
      - BrandPartner.InventorySnapshots
      - BrandPartner.InventoryMovements
      - BrandPartner.SettlementDetails
      - BrandPartner.SettlementHeaders
      - BrandPartner.InventoryItems

    Uso:
      1) Cambia el valor de @AmazonAccountId
      2) Ejecuta el script
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @AmazonAccountId INT = 0; -- TODO: reemplazar (ej. 7)

IF @AmazonAccountId IS NULL OR @AmazonAccountId <= 0
BEGIN
    THROW 50001, 'Debe indicar un @AmazonAccountId valido (> 0).', 1;
END;

BEGIN TRY
    BEGIN TRAN;

    DECLARE @SettlementHeaders TABLE
    (
        SettlementHeaderId BIGINT PRIMARY KEY
    );

    DECLARE @InventoryItems TABLE
    (
        InventoryItemId BIGINT PRIMARY KEY
    );

    INSERT INTO @SettlementHeaders (SettlementHeaderId)
    SELECT sh.SettlementHeaderId
    FROM BrandPartner.SettlementHeaders sh
    WHERE sh.AmazonAccountId = @AmazonAccountId;

    INSERT INTO @InventoryItems (InventoryItemId)
    SELECT ii.InventoryItemId
    FROM BrandPartner.InventoryItems ii
    WHERE ii.AmazonAccountId = @AmazonAccountId;

    -- 1) Snapshots relacionados por SettlementHeader o por InventoryItem
    DELETE s
    FROM BrandPartner.InventorySnapshots s
    WHERE s.SettlementHeaderId IN (SELECT SettlementHeaderId FROM @SettlementHeaders)
       OR s.InventoryItemId IN (SELECT InventoryItemId FROM @InventoryItems);

    -- 2) Movements relacionados por SettlementHeader o por InventoryItem
    DELETE m
    FROM BrandPartner.InventoryMovements m
    WHERE m.SettlementHeaderId IN (SELECT SettlementHeaderId FROM @SettlementHeaders)
       OR m.InventoryItemId IN (SELECT InventoryItemId FROM @InventoryItems);

    -- 3) SettlementDetails de esos headers
    DELETE d
    FROM BrandPartner.SettlementDetails d
    WHERE d.SettlementHeaderId IN (SELECT SettlementHeaderId FROM @SettlementHeaders);

    -- 4) SettlementHeaders del AmazonAccount
    DELETE h
    FROM BrandPartner.SettlementHeaders h
    WHERE h.SettlementHeaderId IN (SELECT SettlementHeaderId FROM @SettlementHeaders);

    -- 5) InventoryItems del AmazonAccount
    DELETE i
    FROM BrandPartner.InventoryItems i
    WHERE i.InventoryItemId IN (SELECT InventoryItemId FROM @InventoryItems);

    COMMIT TRAN;

    SELECT
        1 AS Success,
        @AmazonAccountId AS AmazonAccountId,
        'Datos de pruebas eliminados correctamente para las tablas nuevas de BrandPartner.' AS Message;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrNum INT = ERROR_NUMBER();
    DECLARE @ErrState INT = ERROR_STATE();

    THROW @ErrNum, @ErrMsg, @ErrState;
END CATCH;
