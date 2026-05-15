/*
  Normalización: identidad Brand Partner en Global.Users

  Reglas para NO pisar usuarios corporativos ni duplicar por email:

  • 2a) Solo se copian CustomerId y RequirePasswordChangeOnNextLogin desde
        BrandPartnerUsers hacia Global.Users cuando la fila global es BP “pura”:
        IsBrandPartner = 1 AND IsCorporate = 0 (p. ej. Chelsey, Joe, Ruth nuevos).

  • NO se modifican filas con IsCorporate = 1 (empleados VH o híbridos como Daniel
        o Adrián ya en Global): no se sobrescribe su CustomerId desde BP desde este script.

  • 2b) INSERT sólo si NO existe NADIE en Global con el mismo email (cualquier flag).
        Así jamartinezu90@gmail.com / marenco8@gmail.com entran nuevos sin choque.

  • 4) Mapeo 2FA/historial: join por email a Global.Users sin exigir IsBrandPartner,
        así códigos e historial de daniel@… o adrianfajardo… apuntan al UserId correcto
        (única fila por email en tu catálogo).

  Tras ejecutar 2a/2b, revisa (opcional) el SELECT de “revision” al final:
    usuarios BP con email que coincide con Global CORPORativo → pueden necesitar
    CustomerId en Global a mano si el negocio lo exige.

  Orden FK: primero se eliminan las FK hijas → BrandPartnerUsers; luego el UPDATE del
  paso 4; después renombrar columna y crear FK → Global.Users. Si el paso 4 corre con
  la FK a BrandPartnerUsers activa, SQL Server rechaza valores que son UserId globales.

  Azure SQL Database: usa la versión de este archivo en el repo completo (paso 3 = índice;
  paso 3b = quitar FK inmediatamente después). Si ves QUOTENAME, estás ejecutando texto
  antiguo pegado desde otro sitio — vuelve a copiar desde git.

  Variables @bpFk* justo después de SET NOCOUNT: algunos clientes fallan si DECLARE aparece
  más abajo tras CREATE INDEX sin separar lotes manualmente (GO sólo existe en herramientas
  cliente, no como sentencia T-SQL en Azure Database).

  Si el paso 6 falla con Msg 4902 sobre BrandPartnerUserLoginHistory: la tabla no existe en
  esa base (o sin permisos / BD equivocada). Aplica Scripts/CreateBrandPartnerUsersTable.sql
  o ejecuta este script completo contra la BD donde ya están las tablas Brand Partner.
*/

SET NOCOUNT ON;

DECLARE @bpFkRemain SYSNAME;
DECLARE @bpFkDropDdl NVARCHAR(520);

-- =====================================================================
-- 1) Global.Users: nuevas columnas
-- =====================================================================
IF COL_LENGTH(N'Global.Users', N'CustomerId') IS NULL
    ALTER TABLE [Global].[Users] ADD [CustomerId] INT NULL;

IF COL_LENGTH(N'Global.Users', N'RequirePasswordChangeOnNextLogin') IS NULL
BEGIN
    ALTER TABLE [Global].[Users] ADD [RequirePasswordChangeOnNextLogin] BIT NOT NULL
        CONSTRAINT [DF_Users_RequirePasswordChangeOnNextLogin] DEFAULT (0);
END;

-- =====================================================================
-- 2) Sincronizar desde BrandPartner.BrandPartnerUsers (por email)
-- =====================================================================

-- 2a) Solo usuarios BP puros — no tocar IsCorporate = 1
UPDATE gu
SET
    gu.[CustomerId] = bp.[CustomerId],
    gu.[RequirePasswordChangeOnNextLogin] = bp.[RequirePasswordChangeOnNextLogin]
FROM [Global].[Users] gu
INNER JOIN [BrandPartner].[BrandPartnerUsers] bp
    ON LOWER(LTRIM(RTRIM(gu.[Email]))) = LOWER(LTRIM(RTRIM(bp.[Email])))
WHERE gu.[IsBrandPartner] = 1
  AND gu.[IsCorporate] = 0;

-- 2b) Insertar sólo cuando no exista ese email en Global (ningún registro).
--     Evita duplicar jamartinez… si en el futuro hubiera correlación sólo corporativa.
INSERT INTO [Global].[Users] (
    [FirstName], [LastName], [Email], [Username], [PasswordHash], [PhoneNumber],
    [ProfilePictureUrl], [IsCorporate], [IsBrandPartner], [IsActive], [EmailVerified],
    [FailedLoginAttempts], [LockedUntil], [CustomerId], [RequirePasswordChangeOnNextLogin],
    [CreatedAt], [UpdatedAt], [CreatedBy]
)
SELECT
    bp.[FirstName],
    bp.[LastName],
    bp.[Email],
    bp.[Email] AS [Username],
    bp.[PasswordHash],
    bp.[PhoneNumber],
    NULL,
    0,
    1,
    bp.[IsActive],
    bp.[EmailVerified],
    bp.[FailedLoginAttempts],
    bp.[LockedUntil],
    bp.[CustomerId],
    bp.[RequirePasswordChangeOnNextLogin],
    COALESCE(bp.[CreatedAt], GETUTCDATE()),
    bp.[UpdatedAt],
    bp.[CreatedBy]
FROM [BrandPartner].[BrandPartnerUsers] bp
WHERE NOT EXISTS (
    SELECT 1
    FROM [Global].[Users] gu
    WHERE LOWER(LTRIM(RTRIM(gu.[Email]))) = LOWER(LTRIM(RTRIM(bp.[Email])))
);

-- =====================================================================
-- 3) Índice (opcional, idempotente)
-- =====================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Users_IsBrandPartner_CustomerId' AND object_id = OBJECT_ID(N'Global.Users')
)
    CREATE NONCLUSTERED INDEX [IX_Users_IsBrandPartner_CustomerId]
    ON [Global].[Users] ([IsBrandPartner], [CustomerId]);

-- =====================================================================
-- 3b) Quitar FK a BrandPartnerUsers (Azure SQL Database; antes del remap, paso 4)
--     Nombres canónicos: Scripts/CreateBrandPartnerUsersTable.sql
--     Fallback: sp_executesql + REPLACE (sin QUOTENAME ni EXEC('...').
-- =====================================================================

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'BrandPartner.BrandPartnerTwoFactorCodes')
      AND referenced_object_id = OBJECT_ID(N'BrandPartner.BrandPartnerUsers')
      AND name = N'FK_BrandPartnerTwoFactorCodes_BrandPartnerUsers'
)
BEGIN
    ALTER TABLE [BrandPartner].[BrandPartnerTwoFactorCodes]
    DROP CONSTRAINT [FK_BrandPartnerTwoFactorCodes_BrandPartnerUsers];
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'BrandPartner.BrandPartnerUserLoginHistory')
      AND referenced_object_id = OBJECT_ID(N'BrandPartner.BrandPartnerUsers')
      AND name = N'FK_BrandPartnerUserLoginHistory_BrandPartnerUsers'
)
BEGIN
    ALTER TABLE [BrandPartner.BrandPartnerUserLoginHistory]
    DROP CONSTRAINT [FK_BrandPartnerUserLoginHistory_BrandPartnerUsers];
END;

SET @bpFkRemain = NULL;
SELECT TOP (1)
       @bpFkRemain = fk.[name]
FROM sys.foreign_keys fk
WHERE fk.[parent_object_id] = OBJECT_ID(N'BrandPartner.BrandPartnerTwoFactorCodes')
  AND fk.[referenced_object_id] = OBJECT_ID(N'BrandPartner.BrandPartnerUsers')
ORDER BY fk.[name];

IF @bpFkRemain IS NOT NULL
BEGIN
    SET @bpFkDropDdl =
          N'ALTER TABLE [BrandPartner].[BrandPartnerTwoFactorCodes] DROP CONSTRAINT ['
        + REPLACE(@bpFkRemain, N']', N']]') + N']';
    EXECUTE sp_executesql @bpFkDropDdl;
END;

SET @bpFkRemain = NULL;
SELECT TOP (1)
       @bpFkRemain = fk.[name]
FROM sys.foreign_keys fk
WHERE fk.[parent_object_id] = OBJECT_ID(N'BrandPartner.BrandPartnerUserLoginHistory')
  AND fk.[referenced_object_id] = OBJECT_ID(N'BrandPartner.BrandPartnerUsers')
ORDER BY fk.[name];

IF @bpFkRemain IS NOT NULL
BEGIN
    SET @bpFkDropDdl =
          N'ALTER TABLE [BrandPartner].[BrandPartnerUserLoginHistory] DROP CONSTRAINT ['
        + REPLACE(@bpFkRemain, N']', N']]') + N']';
    EXECUTE sp_executesql @bpFkDropDdl;
END;

-- =====================================================================
-- 4) Mapeo BrandPartnerUserId -> Global UserId (solo por email, una fila Global/email)
-- =====================================================================
IF OBJECT_ID(N'BrandPartner.BrandPartnerTwoFactorCodes', N'U') IS NOT NULL
BEGIN
    UPDATE tfc
    SET tfc.[BrandPartnerUserId] = gu.[UserId]
    FROM [BrandPartner].[BrandPartnerTwoFactorCodes] tfc
    INNER JOIN [BrandPartner].[BrandPartnerUsers] bp ON bp.[BrandPartnerUserId] = tfc.[BrandPartnerUserId]
    INNER JOIN [Global].[Users] gu
        ON LOWER(LTRIM(RTRIM(gu.[Email]))) = LOWER(LTRIM(RTRIM(bp.[Email])));
END
ELSE
    PRINT N'[NormalizeBrandPartnerIdentity] Paso 4 omitido (2FA): no existe [BrandPartner].[BrandPartnerTwoFactorCodes].';

IF OBJECT_ID(N'BrandPartner.BrandPartnerUserLoginHistory', N'U') IS NOT NULL
BEGIN
    UPDATE h
    SET h.[BrandPartnerUserId] = gu.[UserId]
    FROM [BrandPartner].[BrandPartnerUserLoginHistory] h
    INNER JOIN [BrandPartner].[BrandPartnerUsers] bp ON bp.[BrandPartnerUserId] = h.[BrandPartnerUserId]
    INNER JOIN [Global].[Users] gu
        ON LOWER(LTRIM(RTRIM(gu.[Email]))) = LOWER(LTRIM(RTRIM(bp.[Email])));
END
ELSE
    PRINT N'[NormalizeBrandPartnerIdentity] Paso 4 omitido (historial): no existe [BrandPartner].[BrandPartnerUserLoginHistory].';

-- =====================================================================
-- 5) Renombrar columna y FK — TwoFactorCodes
-- =====================================================================
IF OBJECT_ID(N'BrandPartner.BrandPartnerTwoFactorCodes', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'BrandPartner.BrandPartnerTwoFactorCodes', N'UserId') IS NULL
        AND COL_LENGTH(N'BrandPartner.BrandPartnerTwoFactorCodes', N'BrandPartnerUserId') IS NOT NULL
        EXEC sys.sp_rename
            @objname = N'[BrandPartner].[BrandPartnerTwoFactorCodes].[BrandPartnerUserId]',
            @newname = N'UserId',
            @objtype = N'COLUMN';

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BrandPartnerTwoFactorCodes_GlobalUsers'
    )
        ALTER TABLE [BrandPartner].[BrandPartnerTwoFactorCodes]
        ADD CONSTRAINT [FK_BrandPartnerTwoFactorCodes_GlobalUsers]
        FOREIGN KEY ([UserId]) REFERENCES [Global].[Users]([UserId]);
END
ELSE
    PRINT N'[NormalizeBrandPartnerIdentity] Paso 5 omitido: no existe [BrandPartner].[BrandPartnerTwoFactorCodes].';

-- =====================================================================
-- 6) Login history
-- =====================================================================
IF OBJECT_ID(N'BrandPartner.BrandPartnerUserLoginHistory', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'BrandPartner.BrandPartnerUserLoginHistory', N'UserId') IS NULL
        AND COL_LENGTH(N'BrandPartner.BrandPartnerUserLoginHistory', N'BrandPartnerUserId') IS NOT NULL
        EXEC sys.sp_rename
            @objname = N'[BrandPartner].[BrandPartnerUserLoginHistory].[BrandPartnerUserId]',
            @newname = N'UserId',
            @objtype = N'COLUMN';

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_BrandPartnerUserLoginHistory_GlobalUsers'
    )
        ALTER TABLE [BrandPartner].[BrandPartnerUserLoginHistory]
        ADD CONSTRAINT [FK_BrandPartnerUserLoginHistory_GlobalUsers]
        FOREIGN KEY ([UserId]) REFERENCES [Global].[Users]([UserId]);
END
ELSE
    PRINT N'[NormalizeBrandPartnerIdentity] Paso 6 omitido: no existe [BrandPartner].[BrandPartnerUserLoginHistory]. Cree la tabla (p. ej. Scripts/CreateBrandPartnerUsersTable.sql) y vuelva a ejecutar pasos 4–6.';

-- =====================================================================
-- 7) Revisión manual sugerida: BP cuyo email ya era cuenta corporativa
-- =====================================================================
/*
SELECT
    bp.[BrandPartnerUserId],
    bp.[CustomerId],
    bp.[Email],
    gu.[UserId],
    gu.[IsCorporate],
    gu.[IsBrandPartner],
    gu.[CustomerId] AS GlobalCustomerId
FROM [BrandPartner].[BrandPartnerUsers] bp
INNER JOIN [Global].[Users] gu
    ON LOWER(LTRIM(RTRIM(gu.[Email]))) = LOWER(LTRIM(RTRIM(bp.[Email])))
WHERE gu.[IsCorporate] = 1;
*/
