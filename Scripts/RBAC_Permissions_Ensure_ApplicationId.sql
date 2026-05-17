/*
  RBAC: asegurar ApplicationId en [Global].[Permissions]

  La columna ya existe en entornos alineados con el seed actual.
  Este script es idempotente: solo altera si falta la columna o FK.

  Uso:
    - Ejecutar en Azure SQL / VH-DB una vez por ambiente.
    - Luego filtrar en API: GET /api/RBAC/permissions?applicationKey=brandpartner
*/

SET NOCOUNT ON;

-- =====================================================================
-- 1) Columna ApplicationId (si no existe)
-- =====================================================================
IF COL_LENGTH(N'Global.Permissions', N'ApplicationId') IS NULL
BEGIN
    ALTER TABLE [Global].[Permissions]
    ADD [ApplicationId] INT NULL;

    PRINT N'Columna ApplicationId agregada (nullable temporalmente).';
END
ELSE
    PRINT N'Columna ApplicationId ya existe.';

-- =====================================================================
-- 2) Backfill desde Resources (permiso hereda app del recurso)
-- =====================================================================
IF COL_LENGTH(N'Global.Permissions', N'ApplicationId') IS NOT NULL
BEGIN
    UPDATE p
    SET p.[ApplicationId] = r.[ApplicationId]
    FROM [Global].[Permissions] p
    INNER JOIN [Global].[Resources] r ON r.[ResourceId] = p.[ResourceId]
    WHERE p.[ApplicationId] IS NULL;

    PRINT N'Permissions actualizados desde Resources: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
END

-- =====================================================================
-- 3) NOT NULL cuando ya no hay nulos
-- =====================================================================
IF COL_LENGTH(N'Global.Permissions', N'ApplicationId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [Global].[Permissions] WHERE [ApplicationId] IS NULL)
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = N'Global' AND t.name = N'Permissions'
          AND c.name = N'ApplicationId' AND c.is_nullable = 1
    )
    BEGIN
        ALTER TABLE [Global].[Permissions]
        ALTER COLUMN [ApplicationId] INT NOT NULL;

        PRINT N'ApplicationId definido como NOT NULL.';
    END
END
ELSE IF EXISTS (SELECT 1 FROM [Global].[Permissions] WHERE [ApplicationId] IS NULL)
    PRINT N'ADVERTENCIA: quedan Permissions sin ApplicationId. Revise Resources huérfanos antes de NOT NULL.';

-- =====================================================================
-- 4) FK a Applications (si no existe)
-- =====================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_Permissions_Applications'
      AND parent_object_id = OBJECT_ID(N'[Global].[Permissions]')
)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Global].[Permissions] WHERE [ApplicationId] IS NULL)
    BEGIN
        ALTER TABLE [Global].[Permissions]
        ADD CONSTRAINT [FK_Permissions_Applications]
        FOREIGN KEY ([ApplicationId]) REFERENCES [Global].[Applications]([ApplicationId]);

        PRINT N'FK FK_Permissions_Applications creada.';
    END
    ELSE
        PRINT N'FK omitida: hay ApplicationId NULL.';
END
ELSE
    PRINT N'FK FK_Permissions_Applications ya existe.';

-- =====================================================================
-- 5) Índice para filtros por aplicación
-- =====================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Permissions_ApplicationId_IsActive'
      AND object_id = OBJECT_ID(N'[Global].[Permissions]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Permissions_ApplicationId_IsActive]
    ON [Global].[Permissions] ([ApplicationId], [IsActive])
    INCLUDE ([PermissionKey], [ResourceId], [ActionId], [PermissionName]);

    PRINT N'Índice IX_Permissions_ApplicationId_IsActive creado.';
END

PRINT N'RBAC_Permissions_Ensure_ApplicationId completado.';
