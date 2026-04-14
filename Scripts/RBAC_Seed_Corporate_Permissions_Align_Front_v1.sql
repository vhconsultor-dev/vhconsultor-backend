/*
  RBAC – Catálogo Corporate alineado con el frontend (vhconsultor-corporate)
  =========================================================================
  Fuentes de verdad en código:
    - src/config/features.ts   (CORPORATE_FEATURES)
    - src/config/permissions.ts

  Qué hace este script (idempotente):
    0) Resuelve @CorporateAppId desde [Global].[Applications].
    1) LIMPIEZA: elimina todo el RBAC previo de Corporate en orden correcto:
         UserRoles → RolePermissions → Roles → Permissions → Resources
       (las Actions son globales y NO se tocan)
    2) Asegura Actions globales en [Global].[Actions].
    3) Inserta Resources en [Global].[Resources] con ApplicationId = @CorporateAppId.
    4) Inserta Permissions (ResourceId × ActionId → PermissionKey único).
    5) Crea el rol SuperAdmin Corporate y le asigna TODOS los permisos.
    6) Asigna el rol SuperAdmin a los usuarios JOMAUR y adfape.

  Ejecución: SSMS / sqlcmd contra VH-DB (schema Global).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

/* ========================================================================== */
/* 0) ApplicationId para Corporate                                             */
/* ========================================================================== */
DECLARE @CorporateAppId INT;
SELECT @CorporateAppId = ApplicationId
FROM [Global].[Applications]
WHERE ApplicationKey = N'corporate';

IF @CorporateAppId IS NULL
BEGIN
  RAISERROR(N'No se encontró ApplicationId para "corporate" en [Global].[Applications]. Ajusta el script.', 16, 1);
  ROLLBACK TRANSACTION;
  RETURN;
END

PRINT N'ApplicationId Corporate = ' + CAST(@CorporateAppId AS NVARCHAR(20));

/* ========================================================================== */
/* 1) LIMPIEZA – eliminar RBAC previo de Corporate (orden FK)                 */
/* ========================================================================== */
PRINT N'';
PRINT N'--- Limpiando RBAC previo de Corporate ---';

-- 1a) UserRoles: roles Corporate asignados a usuarios
DELETE ur
FROM [Global].[UserRoles] ur
INNER JOIN [Global].[Roles] r ON r.RoleId = ur.RoleId
WHERE r.ApplicationId = @CorporateAppId;

PRINT N'UserRoles eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1b) RolePermissions: permisos asignados a roles Corporate
DELETE rp
FROM [Global].[RolePermissions] rp
INNER JOIN [Global].[Roles] r ON r.RoleId = rp.RoleId
WHERE r.ApplicationId = @CorporateAppId;

PRINT N'RolePermissions eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1c) Roles Corporate
DELETE FROM [Global].[Roles]
WHERE ApplicationId = @CorporateAppId;

PRINT N'Roles eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1d) Permissions cuyo Resource pertenece a Corporate
DELETE p
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @CorporateAppId;

PRINT N'Permissions eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

-- 1e) Resources Corporate
DELETE FROM [Global].[Resources]
WHERE ApplicationId = @CorporateAppId;

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
(N'Page view',          N'page.view',          N'Acceso a la pantalla del módulo'),
(N'Read',               N'read',               N'Consulta / listado'),
(N'Create',             N'create',             N'Alta'),
(N'Update',             N'update',             N'Edición'),
(N'Delete',             N'delete',             N'Baja / desactivación'),
(N'Export',             N'export',             N'Exportación'),
(N'Import',             N'import',             N'Importación'),
(N'Approve',            N'approve',            N'Aprobación'),
(N'Reject',             N'reject',             N'Rechazo'),
(N'Reset password',     N'reset_password',     N'Restablecer contraseña'),
(N'Lock / unlock',      N'lock_unlock',        N'Bloqueo de cuenta'),
(N'Assign permissions', N'assign_permissions', N'Asignar permisos a roles o usuarios');

INSERT INTO [Global].[Actions] (ActionName, ActionKey, Description, IsActive, CreatedAt)
SELECT a.ActionName, a.ActionKey, a.Description, 1, @Now
FROM @Actions a
WHERE NOT EXISTS (
  SELECT 1 FROM [Global].[Actions] x WHERE x.ActionKey = a.ActionKey
);

PRINT N'Actions aseguradas.';

/* ========================================================================== */
/* 3) Resources (un recurso por featureKey Corporate)                         */
/* ========================================================================== */
DECLARE @Resources TABLE (
  ResourceName NVARCHAR(100) NOT NULL,
  ResourceKey  NVARCHAR(50)  NOT NULL,
  Module       NVARCHAR(50)  NULL,
  Description  NVARCHAR(500) NULL
);

INSERT INTO @Resources (ResourceName, ResourceKey, Module, Description) VALUES
(N'Customers',                       N'corporate.customers',                       N'customers', N'Gestión de clientes Corporate'),
(N'Contracts',                       N'corporate.contracts',                       N'contracts', N'Contratos por cliente'),
(N'Invoices',                        N'corporate.invoices',                        N'invoices',  N'Facturas por contrato'),
(N'Billing reports',                 N'corporate.reports.billing',                 N'reports',   N'Reporte de facturación'),
(N'Amazon orders',                   N'corporate.amazon.orders',                   N'amazon',    N'Pedidos Amazon'),
(N'Settings – Corporate users',      N'corporate.settings.users',                  N'settings',  N'Usuarios Corporate'),
(N'Settings – Brand Partner users',  N'corporate.settings.brandpartner_users',     N'settings',  N'Usuarios Brand Partner'),
(N'Settings – Roles',                N'corporate.settings.roles',                  N'settings',  N'Roles'),
(N'Settings – Role permissions',     N'corporate.settings.role_permissions',       N'settings',  N'Matriz rol–permiso'),
(N'Settings – Permissions catalog',  N'corporate.settings.permissions',            N'settings',  N'Catálogo de permisos'),
(N'Settings – Pricing',              N'corporate.settings.pricing',                N'settings',  N'Pricing'),
(N'Settings – Amazon marketplaces',  N'corporate.settings.amazon_marketplaces',    N'settings',  N'Marketplaces Amazon'),
(N'Settings – Amazon account ASINs', N'corporate.settings.amazon_account_asins',   N'settings',  N'ASINs por cuenta Amazon'),
(N'Profile',                         N'corporate.profile',                         N'profile',   N'Perfil de usuario Corporate');

INSERT INTO [Global].[Resources] (ApplicationId, ResourceName, ResourceKey, Description, Module, IsActive, CreatedAt)
SELECT @CorporateAppId, r.ResourceName, r.ResourceKey, r.Description, r.Module, 1, @Now
FROM @Resources r;

PRINT N'Resources insertados: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

/* ========================================================================== */
/* 4) Permissions: (ResourceKey, ActionKey) → PermissionKey                   */
/* ========================================================================== */
DECLARE @PermSpec TABLE (
  ResourceKey    NVARCHAR(50)  NOT NULL,
  ActionKey      NVARCHAR(50)  NOT NULL,
  PermissionKey  NVARCHAR(100) NOT NULL,
  PermissionName NVARCHAR(200) NOT NULL
);

/* --- corporate.customers --- */
INSERT INTO @PermSpec VALUES
(N'corporate.customers', N'page.view', N'corporate.customers.page.view', N'Customers · Page view'),
(N'corporate.customers', N'read',      N'corporate.customers.read',      N'Customers · Read'),
(N'corporate.customers', N'create',    N'corporate.customers.create',    N'Customers · Create'),
(N'corporate.customers', N'update',    N'corporate.customers.update',    N'Customers · Update'),
(N'corporate.customers', N'delete',    N'corporate.customers.delete',    N'Customers · Delete'),
(N'corporate.customers', N'export',    N'corporate.customers.export',    N'Customers · Export'),
(N'corporate.customers', N'import',    N'corporate.customers.import',    N'Customers · Import');

/* --- corporate.contracts --- */
INSERT INTO @PermSpec VALUES
(N'corporate.contracts', N'page.view', N'corporate.contracts.page.view', N'Contracts · Page view'),
(N'corporate.contracts', N'read',      N'corporate.contracts.read',      N'Contracts · Read'),
(N'corporate.contracts', N'create',    N'corporate.contracts.create',    N'Contracts · Create'),
(N'corporate.contracts', N'update',    N'corporate.contracts.update',    N'Contracts · Update'),
(N'corporate.contracts', N'delete',    N'corporate.contracts.delete',    N'Contracts · Delete'),
(N'corporate.contracts', N'approve',   N'corporate.contracts.approve',   N'Contracts · Approve'),
(N'corporate.contracts', N'reject',    N'corporate.contracts.reject',    N'Contracts · Reject');

/* --- corporate.invoices --- */
INSERT INTO @PermSpec VALUES
(N'corporate.invoices', N'page.view', N'corporate.invoices.page.view', N'Invoices · Page view'),
(N'corporate.invoices', N'read',      N'corporate.invoices.read',      N'Invoices · Read'),
(N'corporate.invoices', N'create',    N'corporate.invoices.create',    N'Invoices · Create'),
(N'corporate.invoices', N'update',    N'corporate.invoices.update',    N'Invoices · Update'),
(N'corporate.invoices', N'delete',    N'corporate.invoices.delete',    N'Invoices · Delete'),
(N'corporate.invoices', N'export',    N'corporate.invoices.export',    N'Invoices · Export');

/* --- corporate.reports.billing --- */
INSERT INTO @PermSpec VALUES
(N'corporate.reports.billing', N'page.view', N'corporate.reports.billing.page.view', N'Billing report · Page view'),
(N'corporate.reports.billing', N'read',      N'corporate.reports.billing.read',      N'Billing report · Read'),
(N'corporate.reports.billing', N'export',    N'corporate.reports.billing.export',    N'Billing report · Export');

/* --- corporate.amazon.orders --- */
INSERT INTO @PermSpec VALUES
(N'corporate.amazon.orders', N'page.view', N'corporate.amazon.orders.page.view', N'Amazon orders · Page view'),
(N'corporate.amazon.orders', N'read',      N'corporate.amazon.orders.read',      N'Amazon orders · Read'),
(N'corporate.amazon.orders', N'export',    N'corporate.amazon.orders.export',    N'Amazon orders · Export');

/* --- corporate.settings.users --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.users', N'page.view',          N'corporate.settings.users.page.view',          N'Corporate users · Page view'),
(N'corporate.settings.users', N'read',                N'corporate.settings.users.read',                N'Corporate users · Read'),
(N'corporate.settings.users', N'create',              N'corporate.settings.users.create',              N'Corporate users · Create'),
(N'corporate.settings.users', N'update',              N'corporate.settings.users.update',              N'Corporate users · Update'),
(N'corporate.settings.users', N'delete',              N'corporate.settings.users.delete',              N'Corporate users · Delete'),
(N'corporate.settings.users', N'reset_password',      N'corporate.settings.users.reset_password',      N'Corporate users · Reset password'),
(N'corporate.settings.users', N'lock_unlock',         N'corporate.settings.users.lock_unlock',         N'Corporate users · Lock/unlock'),
(N'corporate.settings.users', N'assign_permissions',  N'corporate.settings.users.assign_permissions',  N'Corporate users · Assign permissions');

/* --- corporate.settings.brandpartner_users --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.brandpartner_users', N'page.view',     N'corporate.settings.brandpartner_users.page.view',     N'BP users · Page view'),
(N'corporate.settings.brandpartner_users', N'read',           N'corporate.settings.brandpartner_users.read',           N'BP users · Read'),
(N'corporate.settings.brandpartner_users', N'create',         N'corporate.settings.brandpartner_users.create',         N'BP users · Create'),
(N'corporate.settings.brandpartner_users', N'update',         N'corporate.settings.brandpartner_users.update',         N'BP users · Update'),
(N'corporate.settings.brandpartner_users', N'delete',         N'corporate.settings.brandpartner_users.delete',         N'BP users · Delete'),
(N'corporate.settings.brandpartner_users', N'reset_password', N'corporate.settings.brandpartner_users.reset_password', N'BP users · Reset password'),
(N'corporate.settings.brandpartner_users', N'lock_unlock',    N'corporate.settings.brandpartner_users.lock_unlock',    N'BP users · Lock/unlock');

/* --- corporate.settings.roles --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.roles', N'page.view',         N'corporate.settings.roles.page.view',         N'Roles · Page view'),
(N'corporate.settings.roles', N'read',               N'corporate.settings.roles.read',               N'Roles · Read'),
(N'corporate.settings.roles', N'create',             N'corporate.settings.roles.create',             N'Roles · Create'),
(N'corporate.settings.roles', N'update',             N'corporate.settings.roles.update',             N'Roles · Update'),
(N'corporate.settings.roles', N'delete',             N'corporate.settings.roles.delete',             N'Roles · Delete'),
(N'corporate.settings.roles', N'assign_permissions', N'corporate.settings.roles.assign_permissions', N'Roles · Assign permissions');

/* --- corporate.settings.role_permissions --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.role_permissions', N'assign_permissions', N'corporate.settings.role_permissions.assign_permissions', N'Role permissions matrix · Assign');

/* --- corporate.settings.permissions --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.permissions', N'page.view', N'corporate.settings.permissions.page.view', N'Permissions catalog · Page view'),
(N'corporate.settings.permissions', N'read',      N'corporate.settings.permissions.read',      N'Permissions catalog · Read'),
(N'corporate.settings.permissions', N'create',    N'corporate.settings.permissions.create',    N'Permissions catalog · Create'),
(N'corporate.settings.permissions', N'update',    N'corporate.settings.permissions.update',    N'Permissions catalog · Update'),
(N'corporate.settings.permissions', N'delete',    N'corporate.settings.permissions.delete',    N'Permissions catalog · Delete');

/* --- corporate.settings.pricing --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.pricing', N'page.view', N'corporate.settings.pricing.page.view', N'Pricing · Page view'),
(N'corporate.settings.pricing', N'read',      N'corporate.settings.pricing.read',      N'Pricing · Read'),
(N'corporate.settings.pricing', N'create',    N'corporate.settings.pricing.create',    N'Pricing · Create'),
(N'corporate.settings.pricing', N'update',    N'corporate.settings.pricing.update',    N'Pricing · Update'),
(N'corporate.settings.pricing', N'delete',    N'corporate.settings.pricing.delete',    N'Pricing · Delete');

/* --- corporate.settings.amazon_marketplaces --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.amazon_marketplaces', N'page.view', N'corporate.settings.amazon_marketplaces.page.view', N'Amazon marketplaces · Page view'),
(N'corporate.settings.amazon_marketplaces', N'read',      N'corporate.settings.amazon_marketplaces.read',      N'Amazon marketplaces · Read'),
(N'corporate.settings.amazon_marketplaces', N'create',    N'corporate.settings.amazon_marketplaces.create',    N'Amazon marketplaces · Create'),
(N'corporate.settings.amazon_marketplaces', N'update',    N'corporate.settings.amazon_marketplaces.update',    N'Amazon marketplaces · Update'),
(N'corporate.settings.amazon_marketplaces', N'delete',    N'corporate.settings.amazon_marketplaces.delete',    N'Amazon marketplaces · Delete');

/* --- corporate.settings.amazon_account_asins --- */
INSERT INTO @PermSpec VALUES
(N'corporate.settings.amazon_account_asins', N'page.view', N'corporate.settings.amazon_account_asins.page.view', N'Amazon ASINs · Page view'),
(N'corporate.settings.amazon_account_asins', N'read',      N'corporate.settings.amazon_account_asins.read',      N'Amazon ASINs · Read'),
(N'corporate.settings.amazon_account_asins', N'create',    N'corporate.settings.amazon_account_asins.create',    N'Amazon ASINs · Create'),
(N'corporate.settings.amazon_account_asins', N'update',    N'corporate.settings.amazon_account_asins.update',    N'Amazon ASINs · Update'),
(N'corporate.settings.amazon_account_asins', N'delete',    N'corporate.settings.amazon_account_asins.delete',    N'Amazon ASINs · Delete'),
(N'corporate.settings.amazon_account_asins', N'import',    N'corporate.settings.amazon_account_asins.import',    N'Amazon ASINs · Import');

/* --- corporate.profile (solo update; sin page.view según features.ts) --- */
INSERT INTO @PermSpec VALUES
(N'corporate.profile', N'update', N'corporate.profile.update', N'Profile · Update');

/* INSERT de Permissions                                                       */
/* Nota: la tabla [Global].[Permissions] tiene columna ApplicationId NOT NULL  */
/* Se toma directamente de @CorporateAppId (mismo que el Resource padre)       */
INSERT INTO [Global].[Permissions] (ApplicationId, ResourceId, ActionId, PermissionName, PermissionKey, Description, IsActive, CreatedAt)
SELECT @CorporateAppId, r.ResourceId, a.ActionId, p.PermissionName, p.PermissionKey, NULL, 1, @Now
FROM @PermSpec p
INNER JOIN [Global].[Resources] r
  ON r.ResourceKey = p.ResourceKey AND r.ApplicationId = @CorporateAppId
INNER JOIN [Global].[Actions] a
  ON a.ActionKey = p.ActionKey;

DECLARE @NewPermissions INT = @@ROWCOUNT;
PRINT N'Permissions insertados: ' + CAST(@NewPermissions AS NVARCHAR(20));

/* ========================================================================== */
/* 5) Rol SuperAdmin Corporate + asignación de TODOS los permisos Corporate   */
/* ========================================================================== */
PRINT N'';
PRINT N'--- Creando rol SuperAdmin Corporate ---';

INSERT INTO [Global].[Roles] (ApplicationId, RoleName, RoleKey, Description, IsSystemRole, IsActive, CreatedAt)
VALUES (@CorporateAppId, N'Super Admin', N'corporate.superadmin', N'Acceso total a todas las funciones de Corporate', 1, 1, @Now);

DECLARE @SuperAdminRoleId INT = SCOPE_IDENTITY();
PRINT N'Rol SuperAdmin creado. RoleId = ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));

-- Asignar TODOS los permisos Corporate al rol SuperAdmin
INSERT INTO [Global].[RolePermissions] (RoleId, PermissionId, GrantedBy, GrantedAt)
SELECT @SuperAdminRoleId, p.PermissionId, NULL, @Now
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @CorporateAppId;

PRINT N'Permisos asignados al SuperAdmin: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

/* ========================================================================== */
/* 6) Asignar rol SuperAdmin a los usuarios JOMAUR, adfape y mmarencoe                   */
/* ========================================================================== */
PRINT N'';
PRINT N'--- Asignando rol SuperAdmin a usuarios ---';

DECLARE @UserId_JOMAUR INT;
DECLARE @UserId_adfape INT;
DECLARE @UserId_mmarencoe INT;

SELECT @UserId_JOMAUR = UserId FROM [Global].[Users] WHERE Username = N'JOMAUR';
SELECT @UserId_adfape = UserId FROM [Global].[Users] WHERE Username = N'adfape';
SELECT @UserId_mmarencoe = UserId FROM [Global].[Users] WHERE Username = N'mmarencoe';

-- Asignar a JOMAUR
IF @UserId_JOMAUR IS NULL
BEGIN
  PRINT N'ADVERTENCIA: Usuario JOMAUR no encontrado en [Global].[Users]. Verificar Username exacto.';
END
ELSE
BEGIN
  INSERT INTO [Global].[UserRoles] (ApplicationId, UserId, RoleId, AssignedBy, AssignedAt, ExpiresAt, IsActive)
  VALUES (@CorporateAppId, @UserId_JOMAUR, @SuperAdminRoleId, NULL, @Now, NULL, 1);
  PRINT N'Rol SuperAdmin asignado a JOMAUR (UserId = ' + CAST(@UserId_JOMAUR AS NVARCHAR(20)) + N').';
END

-- Asignar a mmarencoe
IF @UserId_mmarencoe IS NULL
BEGIN
  PRINT N'ADVERTENCIA: Usuario mmarencoe no encontrado en [Global].[Users]. Verificar Username exacto.';
END
ELSE
BEGIN
  INSERT INTO [Global].[UserRoles] (ApplicationId, UserId, RoleId, AssignedBy, AssignedAt, ExpiresAt, IsActive)
  VALUES (@CorporateAppId, @UserId_mmarencoe, @SuperAdminRoleId, NULL, @Now, NULL, 1);
  PRINT N'Rol SuperAdmin asignado a mmarencoe (UserId = ' + CAST(@UserId_mmarencoe AS NVARCHAR(20)) + N').';
END

-- Asignar a adfape
IF @UserId_adfape IS NULL
BEGIN
  PRINT N'ADVERTENCIA: Usuario adfape no encontrado en [Global].[Users]. Verificar Username exacto.';
END
ELSE
BEGIN
  INSERT INTO [Global].[UserRoles] (ApplicationId, UserId, RoleId, AssignedBy, AssignedAt, ExpiresAt, IsActive)
  VALUES (@CorporateAppId, @UserId_adfape, @SuperAdminRoleId, NULL, @Now, NULL, 1);
  PRINT N'Rol SuperAdmin asignado a adfape (UserId = ' + CAST(@UserId_adfape AS NVARCHAR(20)) + N').';
END

COMMIT TRANSACTION;

PRINT N'';
PRINT N'====================================================================';
PRINT N'RBAC_Seed_Corporate_Permissions_Align_Front_v1 completado.';
PRINT N'  Permissions insertados : ' + CAST(@NewPermissions AS NVARCHAR(20));
PRINT N'  Rol SuperAdmin RoleId  : ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));
PRINT N'====================================================================';
PRINT N'';
PRINT N'Verificación rápida:';
PRINT N'  SELECT * FROM [Global].[Roles]           WHERE ApplicationId = ' + CAST(@CorporateAppId AS NVARCHAR(20));
PRINT N'  SELECT * FROM [Global].[RolePermissions] WHERE RoleId = ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));
PRINT N'  SELECT u.Username, r.RoleName FROM [Global].[UserRoles] ur';
PRINT N'    JOIN [Global].[Users] u ON u.UserId = ur.UserId';
PRINT N'    JOIN [Global].[Roles] r ON r.RoleId = ur.RoleId';
PRINT N'    WHERE ur.RoleId = ' + CAST(@SuperAdminRoleId AS NVARCHAR(20));
