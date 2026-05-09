/*
  RBAC – Catálogo Brand Partner alineado con el frontend (vhconsultor-app)
  =========================================================================
  Fuentes de verdad en código:
    - src/config/features.ts   (BRANDPARTNER_FEATURES)
    - src/config/permissions.ts

  Qué hace este script (idempotente):
    0) Resuelve @BrandPartnerAppId desde [Global].[Applications].
    1) LIMPIEZA: elimina todo el RBAC previo de Brand Partner en orden correcto:
         UserRoles → RolePermissions → Roles → Permissions → Resources
       (las Actions son globales y NO se tocan)
    2) Asegura Actions globales en [Global].[Actions].
    3) Inserta Resources en [Global].[Resources] con ApplicationId = @BrandPartnerAppId.
    4) Inserta Permissions (ResourceId × ActionId → PermissionKey único).
    5) Crea el rol SuperAdmin Brand Partner y le asigna TODOS los permisos.
    6) Asigna el rol SuperAdmin a los usuarios JOMAUR y adfape.

  Ejecución: SSMS / sqlcmd contra VH-DB (schema Global).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

/* ========================================================================== */
/* 0) ApplicationId para Brand Partner                                        */
/* ========================================================================== */
DECLARE @BrandPartnerAppId INT;
SELECT @BrandPartnerAppId = ApplicationId
FROM [Global].[Applications]
WHERE ApplicationKey = N'brandpartner';

IF @BrandPartnerAppId IS NULL
BEGIN
  RAISERROR(N'No se encontró ApplicationId para "brandpartner" en [Global].[Applications]. Ajusta el script.', 16, 1);
  ROLLBACK TRANSACTION;
  RETURN;
END

PRINT N'ApplicationId BrandPartner = ' + CAST(@BrandPartnerAppId AS NVARCHAR(20));

/* ========================================================================== */
/* 1) LIMPIEZA – eliminar RBAC previo de Brand Partner (orden FK)             */
/* ========================================================================== */
PRINT N'';
PRINT N'--- Limpiando RBAC previo de Brand Partner ---';

-- 1a) UserRoles: roles Brand Partner asignados a usuarios
DELETE ur
FROM [Global].[UserRoles] ur
INNER JOIN [Global].[Roles] r ON r.RoleId = ur.RoleId
WHERE r.ApplicationId = @BrandPartnerAppId;

PRINT N'UserRoles eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1b) RolePermissions: permisos asignados a roles Brand Partner
DELETE rp
FROM [Global].[RolePermissions] rp
INNER JOIN [Global].[Roles] r ON r.RoleId = rp.RoleId
WHERE r.ApplicationId = @BrandPartnerAppId;

PRINT N'RolePermissions eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1c) Roles Brand Partner
DELETE FROM [Global].[Roles]
WHERE ApplicationId = @BrandPartnerAppId;

PRINT N'Roles eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1d) Permissions cuyo Resource pertenece a Brand Partner
DELETE p
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @BrandPartnerAppId;

PRINT N'Permissions eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1e) Resources Brand Partner
DELETE FROM [Global].[Resources]
WHERE ApplicationId = @BrandPartnerAppId;

PRINT N'Resources eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

PRINT N'--- Limpieza completada ---';
PRINT N'';

/* ========================================================================== */
/* 2) Actions (globales, sin ApplicationId)                                   */
/* ========================================================================== */
DECLARE @Actions TABLE (
  ActionName  NVARCHAR(100) NOT NULL,
  ActionKey   NVARCHAR(50)  NOT NULL,
  Description NVARCHAR(500) NULL
);

INSERT INTO @Actions (ActionName, ActionKey, Description) VALUES
(N'Page view', N'page.view', N'Acceso a la pantalla del módulo'),
(N'Read',      N'read',      N'Consulta / listado'),
(N'Create',    N'create',    N'Alta'),
(N'Update',    N'update',    N'Edición'),
(N'Delete',    N'delete',    N'Baja / desactivación'),
(N'Export',    N'export',    N'Exportación'),
(N'Import',    N'import',    N'Importación');

INSERT INTO [Global].[Actions] (ActionName, ActionKey, Description, IsActive, CreatedAt)
SELECT a.ActionName, a.ActionKey, a.Description, 1, @Now
FROM @Actions a
WHERE NOT EXISTS (
  SELECT 1 FROM [Global].[Actions] x WHERE x.ActionKey = a.ActionKey
);

PRINT N'Actions aseguradas.';

/* ========================================================================== */
/* 3) Resources (un recurso por featureKey Brand Partner único)               */
/* ========================================================================== */
DECLARE @Resources TABLE (
  ResourceName NVARCHAR(100) NOT NULL,
  ResourceKey  NVARCHAR(100) NOT NULL,
  Module       NVARCHAR(50)  NULL,
  Description  NVARCHAR(500) NULL
);

INSERT INTO @Resources (ResourceName, ResourceKey, Module, Description) VALUES
(N'Amazon Orders',               N'brandpartner.amazon.orders',                       N'amazon',     N'Pedidos Amazon del Brand Partner'),
(N'Amazon Inventory',            N'brandpartner.amazon.inventory',                    N'amazon',     N'Gestión de inventario Amazon'),
(N'Amazon Buy Box',              N'brandpartner.amazon.buybox',                       N'amazon',     N'Buy Box monitor'),
(N'Vendor Reports Hub',          N'brandpartner.amazon.vendor.reports',               N'amazon',     N'Hub de reportes de Vendor'),
(N'Vendor Traffic Report',       N'brandpartner.amazon.vendor.traffic',               N'amazon',     N'Reporte de tráfico de Vendor'),
(N'Vendor Inventory Report',     N'brandpartner.amazon.vendor.inventory.report',      N'amazon',     N'Reporte de inventario de Vendor'),
(N'Settlement History',          N'brandpartner.amazon.settlement',                   N'amazon',     N'Historial de liquidaciones Amazon'),
(N'Profile',                     N'brandpartner.profile',                             N'profile',    N'Perfil de usuario Brand Partner');

INSERT INTO [Global].[Resources] (ApplicationId, ResourceName, ResourceKey, Description, Module, IsActive, CreatedAt)
SELECT @BrandPartnerAppId, r.ResourceName, r.ResourceKey, r.Description, r.Module, 1, @Now
FROM @Resources r;

PRINT N'Resources insertados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

/* ========================================================================== */
/* 4) Permissions: (ResourceKey, ActionKey) → PermissionKey                   */
/* ========================================================================== */
DECLARE @PermSpec TABLE (
  ResourceKey    NVARCHAR(100) NOT NULL,
  ActionKey      NVARCHAR(50)  NOT NULL,
  PermissionKey  NVARCHAR(150) NOT NULL,
  PermissionName NVARCHAR(200) NOT NULL
);

/* --- brandpartner.amazon.orders --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.orders', N'page.view', N'brandpartner.amazon.orders.page.view', N'Amazon Orders · Page view'),
(N'brandpartner.amazon.orders', N'read',      N'brandpartner.amazon.orders.read',      N'Amazon Orders · Read'),
(N'brandpartner.amazon.orders', N'export',    N'brandpartner.amazon.orders.export',    N'Amazon Orders · Export');

/* --- brandpartner.amazon.inventory --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.inventory', N'page.view', N'brandpartner.amazon.inventory.page.view', N'Amazon Inventory · Page view'),
(N'brandpartner.amazon.inventory', N'read',      N'brandpartner.amazon.inventory.read',      N'Amazon Inventory · Read'),
(N'brandpartner.amazon.inventory', N'update',    N'brandpartner.amazon.inventory.update',    N'Amazon Inventory · Update'),
(N'brandpartner.amazon.inventory', N'export',    N'brandpartner.amazon.inventory.export',    N'Amazon Inventory · Export'),
(N'brandpartner.amazon.inventory', N'import',    N'brandpartner.amazon.inventory.import',    N'Amazon Inventory · Import');

/* --- brandpartner.amazon.buybox --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.buybox', N'page.view', N'brandpartner.amazon.buybox.page.view', N'Buy Box · Page view'),
(N'brandpartner.amazon.buybox', N'read',      N'brandpartner.amazon.buybox.read',      N'Buy Box · Read');

/* --- brandpartner.amazon.vendor.reports --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.vendor.reports', N'page.view', N'brandpartner.amazon.vendor.reports.page.view', N'Vendor Reports Hub · Page view'),
(N'brandpartner.amazon.vendor.reports', N'read',      N'brandpartner.amazon.vendor.reports.read',      N'Vendor Reports Hub · Read');

/* --- brandpartner.amazon.vendor.traffic --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.vendor.traffic', N'page.view', N'brandpartner.amazon.vendor.traffic.page.view', N'Vendor Traffic Report · Page view'),
(N'brandpartner.amazon.vendor.traffic', N'read',      N'brandpartner.amazon.vendor.traffic.read',      N'Vendor Traffic Report · Read');

/* --- brandpartner.amazon.vendor.inventory.report --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.vendor.inventory.report', N'page.view', N'brandpartner.amazon.vendor.inventory.report.page.view', N'Vendor Inventory Report · Page view'),
(N'brandpartner.amazon.vendor.inventory.report', N'read',      N'brandpartner.amazon.vendor.inventory.report.read',      N'Vendor Inventory Report · Read');

/* --- brandpartner.amazon.settlement --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.amazon.settlement', N'page.view', N'brandpartner.amazon.settlement.page.view', N'Settlement History · Page view'),
(N'brandpartner.amazon.settlement', N'read',      N'brandpartner.amazon.settlement.read',      N'Settlement History · Read'),
(N'brandpartner.amazon.settlement', N'export',    N'brandpartner.amazon.settlement.export',    N'Settlement History · Export');

/* --- brandpartner.profile (solo update; sin page.view según features.ts) --- */
INSERT INTO @PermSpec VALUES
(N'brandpartner.profile', N'update', N'brandpartner.profile.update', N'Profile · Update');

/* INSERT de Permissions */
INSERT INTO [Global].[Permissions] (ApplicationId, ResourceId, ActionId, PermissionName, PermissionKey, Description, IsActive, CreatedAt)
SELECT @BrandPartnerAppId, r.ResourceId, a.ActionId, p.PermissionName, p.PermissionKey, NULL, 1, @Now
FROM @PermSpec p
INNER JOIN [Global].[Resources] r
  ON r.ResourceKey = p.ResourceKey AND r.ApplicationId = @BrandPartnerAppId
INNER JOIN [Global].[Actions] a
  ON a.ActionKey = p.ActionKey;

DECLARE @NewPermissions INT = @@ROWCOUNT;
PRINT N'Permissions insertados: ' + CAST(@NewPermissions AS NVARCHAR(20));

/* ========================================================================== */
/* 5) Rol SuperAdmin Brand Partner + asignación de TODOS los permisos         */
/* ========================================================================== */
PRINT N'';
PRINT N'--- Creando rol SuperAdmin Brand Partner ---';

INSERT INTO [Global].[Roles] (ApplicationId, RoleName, RoleKey, Description, IsSystemRole, IsActive, CreatedAt)
VALUES (@BrandPartnerAppId, N'Super Admin', N'brandpartner.superadmin', N'Acceso total a todas las funciones de Brand Partner', 1, 1, @Now);

DECLARE @SuperAdminRoleId INT = SCOPE_IDENTITY();
PRINT N'Rol SuperAdmin creado. RoleId = ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));

-- Asignar TODOS los permisos Brand Partner al rol SuperAdmin
INSERT INTO [Global].[RolePermissions] (RoleId, PermissionId, GrantedBy, GrantedAt)
SELECT @SuperAdminRoleId, p.PermissionId, NULL, @Now
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @BrandPartnerAppId;

PRINT N'Permisos asignados al SuperAdmin: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

/* ========================================================================== */
/* 6) Asignar rol SuperAdmin a los usuarios JOMAUR y adfape                   */
/* ========================================================================== */
PRINT N'';
PRINT N'--- Asignando rol SuperAdmin a usuarios ---';

DECLARE @UserId_JOMAUR INT;
DECLARE @UserId_adfape INT;
DECLARE @UserId_mmarencoe INT;

SELECT @UserId_JOMAUR    = UserId FROM [Global].[Users] WHERE Username = N'JOMAUR';
SELECT @UserId_adfape    = UserId FROM [Global].[Users] WHERE Username = N'adfape';
SELECT @UserId_mmarencoe = UserId FROM [Global].[Users] WHERE Username = N'mmarencoe';

-- Asignar a JOMAUR
IF @UserId_JOMAUR IS NULL
BEGIN
  PRINT N'ADVERTENCIA: Usuario JOMAUR no encontrado en [Global].[Users].';
END
ELSE
BEGIN
  INSERT INTO [Global].[UserRoles] (ApplicationId, UserId, RoleId, AssignedBy, AssignedAt, ExpiresAt, IsActive)
  VALUES (@BrandPartnerAppId, @UserId_JOMAUR, @SuperAdminRoleId, NULL, @Now, NULL, 1);
  PRINT N'Rol SuperAdmin asignado a JOMAUR (UserId = ' + CAST(@UserId_JOMAUR AS NVARCHAR(20)) + N').';
END

-- Asignar a adfape
IF @UserId_adfape IS NULL
BEGIN
  PRINT N'ADVERTENCIA: Usuario adfape no encontrado en [Global].[Users].';
END
ELSE
BEGIN
  INSERT INTO [Global].[UserRoles] (ApplicationId, UserId, RoleId, AssignedBy, AssignedAt, ExpiresAt, IsActive)
  VALUES (@BrandPartnerAppId, @UserId_adfape, @SuperAdminRoleId, NULL, @Now, NULL, 1);
  PRINT N'Rol SuperAdmin asignado a adfape (UserId = ' + CAST(@UserId_adfape AS NVARCHAR(20)) + N').';
END

-- Asignar a mmarencoe
IF @UserId_mmarencoe IS NULL
BEGIN
  PRINT N'ADVERTENCIA: Usuario mmarencoe no encontrado en [Global].[Users].';
END
ELSE
BEGIN
  INSERT INTO [Global].[UserRoles] (ApplicationId, UserId, RoleId, AssignedBy, AssignedAt, ExpiresAt, IsActive)
  VALUES (@BrandPartnerAppId, @UserId_mmarencoe, @SuperAdminRoleId, NULL, @Now, NULL, 1);
  PRINT N'Rol SuperAdmin asignado a mmarencoe (UserId = ' + CAST(@UserId_mmarencoe AS NVARCHAR(20)) + N').';
END

COMMIT TRANSACTION;

PRINT N'';
PRINT N'====================================================================';
PRINT N'RBAC_Seed_BrandPartner_Permissions_Align_Front_v1 completado.';
PRINT N'  Permissions insertados : ' + CAST(@NewPermissions AS NVARCHAR(20));
PRINT N'  Rol SuperAdmin RoleId  : ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));
PRINT N'====================================================================';
PRINT N'';
PRINT N'Verificación rápida:';
PRINT N'  SELECT * FROM [Global].[Roles]           WHERE ApplicationId = ' + CAST(@BrandPartnerAppId AS NVARCHAR(20));
PRINT N'  SELECT * FROM [Global].[RolePermissions] WHERE RoleId = ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));
PRINT N'  SELECT u.Username, r.RoleName FROM [Global].[UserRoles] ur';
PRINT N'    JOIN [Global].[Users] u ON u.UserId = ur.UserId';
PRINT N'    JOIN [Global].[Roles] r ON r.RoleId = ur.RoleId';
PRINT N'    WHERE ur.RoleId = ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));
