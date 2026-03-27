-- =============================================================================
-- Script: RBAC Seed - Corporate Application v2
-- Descripción: Inserta Resources, Actions, Permissions y Roles iniciales
--              para la aplicación Corporate siguiendo la convención
--              corporate.<modulo>.<submodulo?>.<accion>
--
-- Fecha: 2026-03-26
-- Sincronizado con: src/config/features.ts y src/config/permissions.ts
--
-- INSTRUCCIONES:
--   1. Ejecutar en VH-DB con un usuario que tenga permisos en schema [Global]
--   2. Es idempotente: usa IF NOT EXISTS en cada INSERT
--   3. Al final asigna todos los permisos al rol corporate.super_admin
--   4. Los permisos de features "planned" se insertan como IsActive = 0
--      hasta que tengan pantalla real
-- =============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY

-- =============================================================================
-- 0. VARIABLES DE CONTROL
-- =============================================================================

DECLARE @CorporateAppId   INT;
DECLARE @Now              DATETIME = GETDATE();

-- Obtener el ApplicationId de Corporate
SELECT @CorporateAppId = ApplicationId
FROM [Global].[Applications]
WHERE ApplicationKey = 'corporate' AND IsActive = 1;

IF @CorporateAppId IS NULL
BEGIN
    -- Crear la aplicación si no existe aún
    INSERT INTO [Global].[Applications]
        (ApplicationName, ApplicationKey, Description, IsActive, CreatedAt)
    VALUES
        ('Corporate', 'corporate', 'Aplicación de gestión interna VH Consultor', 1, @Now);
    SET @CorporateAppId = SCOPE_IDENTITY();
    PRINT 'Application "corporate" creada con ID: ' + CAST(@CorporateAppId AS VARCHAR);
END
ELSE
    PRINT 'Application "corporate" existente con ID: ' + CAST(@CorporateAppId AS VARCHAR);

-- =============================================================================
-- 1. ACTIONS
-- Todas las acciones disponibles en el sistema.
-- Son globales (no pertenecen a una app) y se reutilizan por todos los recursos.
-- =============================================================================

PRINT '';
PRINT '=== Insertando Actions ===';

;WITH ActionsToInsert AS (
    SELECT * FROM (VALUES
        ('View Page',        'page.view',         'Accede a la página / ruta'),
        ('Read',             'read',              'Leer y listar registros'),
        ('Create',           'create',            'Crear nuevos registros'),
        ('Update',           'update',            'Editar registros existentes'),
        ('Delete',           'delete',            'Eliminar registros'),
        ('Export',           'export',            'Exportar datos a archivo'),
        ('Import',           'import',            'Importar datos desde archivo'),
        ('Approve',          'approve',           'Aprobar un registro'),
        ('Reject',           'reject',            'Rechazar un registro'),
        ('Reset Password',   'reset_password',    'Resetear contraseña de un usuario'),
        ('Lock / Unlock',    'lock_unlock',       'Bloquear o desbloquear una cuenta'),
        ('Assign Permissions','assign_permissions','Asignar permisos a usuario o rol'),
        ('Assign Roles',     'assign_roles',      'Asignar roles a un usuario'),
        ('Manage Denials',   'manage_denials',    'Gestionar denegaciones explícitas')
    ) AS v (ActionName, ActionKey, Description)
)
INSERT INTO [Global].[Actions] (ActionName, ActionKey, Description, IsActive, CreatedAt)
SELECT a.ActionName, a.ActionKey, a.Description, 1, @Now
FROM ActionsToInsert a
WHERE NOT EXISTS (
    SELECT 1 FROM [Global].[Actions] x WHERE x.ActionKey = a.ActionKey
);

PRINT 'Actions procesadas.';

-- =============================================================================
-- 2. RESOURCES
-- Un Resource por feature/módulo de Corporate.
-- Module sirve para agrupar en el panel RBAC admin.
-- Features "planned" (sin pantalla aún) tienen IsActive = 0.
-- =============================================================================

PRINT '';
PRINT '=== Insertando Resources ===';

;WITH ResourcesToInsert AS (
    SELECT * FROM (VALUES
        -- ResourceName                          ResourceKey                                   Description                                        Module              IsActive
        ('Corporate - Home',                    'corporate.home',                             'Página de inicio',                                'main',             1),
        ('Corporate - Customers',               'corporate.customers',                        'Gestión de clientes',                             'main',             1),
        ('Corporate - Contracts',               'corporate.contracts',                        'Gestión de contratos de clientes',                'main',             1),
        ('Corporate - Invoices',                'corporate.invoices',                         'Gestión de facturas de contratos',                'main',             1),
        ('Corporate - Reports Billing',         'corporate.reports.billing',                  'Reporte de facturación',                          'reports',          1),
        ('Corporate - Amazon Orders',           'corporate.amazon.orders',                    'Órdenes de Amazon',                               'amazon',           1),
        ('Corporate - Profile',                 'corporate.profile',                          'Perfil del usuario',                              'profile',          1),
        -- Settings - Users
        ('Corporate - Settings Users',          'corporate.settings.users',                   'Gestión de usuarios corporativos',                'settings',         1),
        ('Corporate - Settings BP Users',       'corporate.settings.brandpartner_users',      'Gestión de usuarios Brand Partner',               'settings',         1),
        -- Settings - RBAC
        ('Corporate - Settings Roles',          'corporate.settings.roles',                   'Gestión de roles del sistema',                    'settings',         1),
        ('Corporate - Settings Permissions',    'corporate.settings.permissions',             'Gestión de permisos del sistema',                 'settings',         1),
        -- Settings - Business
        ('Corporate - Settings Pricing',        'corporate.settings.pricing',                 'Configuración de precios',                        'settings',         1),
        ('Corporate - Settings Amazon Markets', 'corporate.settings.amazon_marketplaces',     'Configuración de marketplaces de Amazon',          'settings',         1),
        ('Corporate - Settings Amazon ASINs',   'corporate.settings.amazon_account_asins',    'Configuración de ASINs por cuenta Amazon',         'settings',         1),
        -- Planned (sin pantalla aún - IsActive = 0)
        ('Corporate - Customer Add',            'corporate.customers.add',                    'Formulario para agregar cliente (planned)',        'main',             0),
        ('Corporate - Customer Reports',        'corporate.customers.reports',                'Reportes de clientes (planned)',                   'main',             0),
        ('Corporate - Customer Settings',       'corporate.customers.settings',               'Configuración de clientes (planned)',              'main',             0),
        -- Futuros - backend ya existe, pantalla pendiente (IsActive = 0)
        ('Corporate - Services',                'corporate.settings.services',                'Gestión de servicios contratables',               'settings',         0),
        ('Corporate - Platforms',               'corporate.settings.platforms',               'Gestión de plataformas',                          'settings',         0),
        ('Corporate - Payment Methods',         'corporate.settings.payment_methods',         'Gestión de métodos de pago',                      'settings',         0),
        ('Corporate - Fee Types',               'corporate.settings.fee_types',               'Gestión de tipos de fee',                         'settings',         0),
        ('Corporate - Contract Types',          'corporate.settings.contract_types',          'Gestión de tipos de contrato',                    'settings',         0),
        ('Corporate - Business Types',          'corporate.settings.business_types',          'Gestión de tipos de negocio',                     'settings',         0),
        ('Corporate - Pricing Services',        'corporate.settings.pricing_services',        'Sub-módulo de servicios de pricing',              'settings',         0),
        ('Corporate - Amazon Vendor Reports',   'corporate.amazon.vendor_reports',            'Reportes de Amazon Vendor',                       'reports',          0),
        ('Corporate - Summary Dashboard',       'corporate.home.summary',                     'Panel resumen / dashboard (planned)',              'main',             0)
    ) AS v (ResourceName, ResourceKey, Description, Module, IsActive)
)
INSERT INTO [Global].[Resources]
    (ApplicationId, ResourceName, ResourceKey, Description, Module, IsActive, CreatedAt)
SELECT @CorporateAppId, r.ResourceName, r.ResourceKey, r.Description, r.Module, r.IsActive, @Now
FROM ResourcesToInsert r
WHERE NOT EXISTS (
    SELECT 1 FROM [Global].[Resources] x WHERE x.ResourceKey = r.ResourceKey
);

PRINT 'Resources procesados.';

-- =============================================================================
-- 3. PERMISSIONS
-- Combinación Resource × Action = Permission con su PermissionKey.
-- Solo se generan los permisos que tienen sentido para cada recurso.
-- =============================================================================

PRINT '';
PRINT '=== Insertando Permissions ===';

;WITH PermissionsToInsert AS (
    SELECT * FROM (VALUES

        -- ── HOME (solo autenticación, sin permisos de permiso propios) ─────────
        -- (no se generan permisos para corporate.home)

        -- ── CUSTOMERS ─────────────────────────────────────────────────────────
        ('corporate.customers.page.view',   'corporate.customers',   'page.view',    'Acceder a la página de clientes',                  1),
        ('corporate.customers.read',        'corporate.customers',   'read',         'Listar y consultar clientes',                       1),
        ('corporate.customers.create',      'corporate.customers',   'create',       'Crear nuevos clientes',                             1),
        ('corporate.customers.update',      'corporate.customers',   'update',       'Editar datos de clientes',                          1),
        ('corporate.customers.delete',      'corporate.customers',   'delete',       'Eliminar clientes',                                 1),
        ('corporate.customers.export',      'corporate.customers',   'export',       'Exportar listado de clientes',                      1),
        ('corporate.customers.import',      'corporate.customers',   'import',       'Importar clientes desde archivo',                   1),

        -- ── CONTRACTS ─────────────────────────────────────────────────────────
        ('corporate.contracts.page.view',   'corporate.contracts',   'page.view',    'Acceder a la página de contratos',                  1),
        ('corporate.contracts.read',        'corporate.contracts',   'read',         'Listar y consultar contratos',                      1),
        ('corporate.contracts.create',      'corporate.contracts',   'create',       'Crear nuevos contratos',                            1),
        ('corporate.contracts.update',      'corporate.contracts',   'update',       'Editar contratos',                                  1),
        ('corporate.contracts.delete',      'corporate.contracts',   'delete',       'Eliminar contratos',                                1),
        ('corporate.contracts.approve',     'corporate.contracts',   'approve',      'Aprobar contratos',                                 1),
        ('corporate.contracts.reject',      'corporate.contracts',   'reject',       'Rechazar contratos',                                1),

        -- ── INVOICES ──────────────────────────────────────────────────────────
        ('corporate.invoices.page.view',    'corporate.invoices',    'page.view',    'Acceder a la página de facturas',                   1),
        ('corporate.invoices.read',         'corporate.invoices',    'read',         'Listar y consultar facturas',                       1),
        ('corporate.invoices.create',       'corporate.invoices',    'create',       'Crear nuevas facturas',                             1),
        ('corporate.invoices.update',       'corporate.invoices',    'update',       'Editar facturas',                                   1),
        ('corporate.invoices.delete',       'corporate.invoices',    'delete',       'Eliminar facturas',                                 1),
        ('corporate.invoices.export',       'corporate.invoices',    'export',       'Exportar facturas',                                 1),

        -- ── REPORTS / BILLING ─────────────────────────────────────────────────
        ('corporate.reports.billing.page.view','corporate.reports.billing','page.view','Acceder al reporte de facturación',               1),
        ('corporate.reports.billing.read',  'corporate.reports.billing','read',       'Consultar datos del reporte de facturación',       1),
        ('corporate.reports.billing.export','corporate.reports.billing','export',     'Exportar reporte de facturación',                  1),

        -- ── AMAZON ORDERS ─────────────────────────────────────────────────────
        ('corporate.amazon.orders.page.view','corporate.amazon.orders','page.view',  'Acceder a la página de órdenes Amazon',             1),
        ('corporate.amazon.orders.read',    'corporate.amazon.orders','read',        'Consultar órdenes de Amazon',                       1),
        ('corporate.amazon.orders.export',  'corporate.amazon.orders','export',      'Exportar órdenes de Amazon',                        1),

        -- ── PROFILE ───────────────────────────────────────────────────────────
        ('corporate.profile.update',        'corporate.profile',     'update',       'Actualizar perfil propio',                          1),

        -- ── SETTINGS / CORPORATE USERS ────────────────────────────────────────
        ('corporate.settings.users.page.view',          'corporate.settings.users','page.view',          'Acceder a gestión de usuarios corporativos',        1),
        ('corporate.settings.users.read',               'corporate.settings.users','read',               'Listar usuarios corporativos',                       1),
        ('corporate.settings.users.create',             'corporate.settings.users','create',             'Crear usuarios corporativos',                        1),
        ('corporate.settings.users.update',             'corporate.settings.users','update',             'Editar usuarios corporativos',                       1),
        ('corporate.settings.users.delete',             'corporate.settings.users','delete',             'Eliminar usuarios corporativos',                     1),
        ('corporate.settings.users.reset_password',     'corporate.settings.users','reset_password',     'Resetear contraseña de usuarios corporativos',      1),
        ('corporate.settings.users.lock_unlock',        'corporate.settings.users','lock_unlock',        'Bloquear/desbloquear usuarios corporativos',         1),
        ('corporate.settings.users.assign_permissions', 'corporate.settings.users','assign_permissions', 'Asignar permisos a usuarios corporativos',           1),

        -- ── SETTINGS / BRAND PARTNER USERS ───────────────────────────────────
        ('corporate.settings.brandpartner_users.page.view',     'corporate.settings.brandpartner_users','page.view',      'Acceder a gestión de usuarios Brand Partner',   1),
        ('corporate.settings.brandpartner_users.read',          'corporate.settings.brandpartner_users','read',           'Listar usuarios Brand Partner',                  1),
        ('corporate.settings.brandpartner_users.create',        'corporate.settings.brandpartner_users','create',         'Crear usuarios Brand Partner',                   1),
        ('corporate.settings.brandpartner_users.update',        'corporate.settings.brandpartner_users','update',         'Editar usuarios Brand Partner',                  1),
        ('corporate.settings.brandpartner_users.delete',        'corporate.settings.brandpartner_users','delete',         'Eliminar usuarios Brand Partner',                1),
        ('corporate.settings.brandpartner_users.reset_password','corporate.settings.brandpartner_users','reset_password', 'Resetear contraseña usuarios Brand Partner',     1),
        ('corporate.settings.brandpartner_users.lock_unlock',   'corporate.settings.brandpartner_users','lock_unlock',    'Bloquear/desbloquear usuarios Brand Partner',    1),

        -- ── SETTINGS / ROLES ─────────────────────────────────────────────────
        ('corporate.settings.roles.page.view',          'corporate.settings.roles','page.view',          'Acceder a gestión de roles',                        1),
        ('corporate.settings.roles.read',               'corporate.settings.roles','read',               'Listar roles',                                      1),
        ('corporate.settings.roles.create',             'corporate.settings.roles','create',             'Crear roles',                                       1),
        ('corporate.settings.roles.update',             'corporate.settings.roles','update',             'Editar roles',                                      1),
        ('corporate.settings.roles.delete',             'corporate.settings.roles','delete',             'Eliminar roles',                                    1),
        ('corporate.settings.roles.assign_permissions', 'corporate.settings.roles','assign_permissions', 'Asignar permisos a roles',                          1),

        -- ── SETTINGS / PERMISSIONS ───────────────────────────────────────────
        ('corporate.settings.permissions.page.view',    'corporate.settings.permissions','page.view',    'Acceder a gestión de permisos',                     1),
        ('corporate.settings.permissions.read',         'corporate.settings.permissions','read',         'Listar permisos',                                   1),
        ('corporate.settings.permissions.create',       'corporate.settings.permissions','create',       'Crear permisos',                                    1),
        ('corporate.settings.permissions.update',       'corporate.settings.permissions','update',       'Editar permisos',                                   1),
        ('corporate.settings.permissions.delete',       'corporate.settings.permissions','delete',       'Eliminar permisos',                                 1),

        -- ── SETTINGS / PRICING ───────────────────────────────────────────────
        ('corporate.settings.pricing.page.view',        'corporate.settings.pricing','page.view',        'Acceder a gestión de precios',                      1),
        ('corporate.settings.pricing.read',             'corporate.settings.pricing','read',             'Consultar precios',                                 1),
        ('corporate.settings.pricing.create',           'corporate.settings.pricing','create',           'Crear tarifas',                                     1),
        ('corporate.settings.pricing.update',           'corporate.settings.pricing','update',           'Editar tarifas',                                    1),
        ('corporate.settings.pricing.delete',           'corporate.settings.pricing','delete',           'Eliminar tarifas',                                  1),

        -- ── SETTINGS / AMAZON MARKETPLACES ───────────────────────────────────
        ('corporate.settings.amazon_marketplaces.page.view','corporate.settings.amazon_marketplaces','page.view','Acceder a gestión de marketplaces',          1),
        ('corporate.settings.amazon_marketplaces.read',     'corporate.settings.amazon_marketplaces','read',     'Listar marketplaces',                        1),
        ('corporate.settings.amazon_marketplaces.create',   'corporate.settings.amazon_marketplaces','create',   'Crear marketplaces',                         1),
        ('corporate.settings.amazon_marketplaces.update',   'corporate.settings.amazon_marketplaces','update',   'Editar marketplaces',                        1),
        ('corporate.settings.amazon_marketplaces.delete',   'corporate.settings.amazon_marketplaces','delete',   'Eliminar marketplaces',                      1),

        -- ── SETTINGS / AMAZON ACCOUNT ASINs ──────────────────────────────────
        ('corporate.settings.amazon_account_asins.page.view','corporate.settings.amazon_account_asins','page.view','Acceder a gestión de ASINs',               1),
        ('corporate.settings.amazon_account_asins.read',     'corporate.settings.amazon_account_asins','read',    'Listar ASINs',                              1),
        ('corporate.settings.amazon_account_asins.create',   'corporate.settings.amazon_account_asins','create',  'Crear ASINs',                               1),
        ('corporate.settings.amazon_account_asins.update',   'corporate.settings.amazon_account_asins','update',  'Editar ASINs',                              1),
        ('corporate.settings.amazon_account_asins.delete',   'corporate.settings.amazon_account_asins','delete',  'Eliminar ASINs',                            1),
        ('corporate.settings.amazon_account_asins.import',   'corporate.settings.amazon_account_asins','import',  'Importar ASINs desde archivo',              1),

        -- ── PLANNED / FUTURE (IsActive = 0) ──────────────────────────────────
        ('corporate.customers.add.page.view',           'corporate.customers.add',    'page.view',        'Acceder al formulario Add Customer (planned)',      0),
        ('corporate.customers.add.create',              'corporate.customers.add',    'create',           'Crear cliente desde formulario Add (planned)',      0),
        ('corporate.customers.reports.page.view',       'corporate.customers.reports','page.view',        'Acceder a reportes de clientes (planned)',          0),
        ('corporate.customers.reports.read',            'corporate.customers.reports','read',             'Consultar reportes de clientes (planned)',          0),
        ('corporate.customers.reports.export',          'corporate.customers.reports','export',           'Exportar reportes de clientes (planned)',           0),
        ('corporate.customers.settings.page.view',      'corporate.customers.settings','page.view',       'Acceder a configuración de clientes (planned)',     0)

    ) AS v (PermissionKey, ResourceKey, ActionKey, Description, IsActive)
)
INSERT INTO [Global].[Permissions]
    (ApplicationId, ResourceId, ActionId, PermissionName, PermissionKey, Description, IsActive, CreatedAt)
SELECT
    @CorporateAppId,
    r.ResourceId,
    a.ActionId,
    p.PermissionKey,   -- PermissionName = PermissionKey por simplicidad
    p.PermissionKey,
    p.Description,
    p.IsActive,
    @Now
FROM PermissionsToInsert p
INNER JOIN [Global].[Resources] r ON r.ResourceKey = p.ResourceKey
INNER JOIN [Global].[Actions]   a ON a.ActionKey   = p.ActionKey
WHERE NOT EXISTS (
    SELECT 1 FROM [Global].[Permissions] x WHERE x.PermissionKey = p.PermissionKey
);

PRINT 'Permissions procesados.';

-- =============================================================================
-- 4. ROLES INICIALES
-- =============================================================================

PRINT '';
PRINT '=== Insertando Roles ===';

;WITH RolesToInsert AS (
    SELECT * FROM (VALUES
        ('Corporate Super Admin',    'corporate.super_admin',    'Acceso completo a toda la aplicación Corporate',                   1),
        ('Corporate Admin',          'corporate.admin',          'Administración de usuarios, roles y configuración',                1),
        ('Corporate Manager',        'corporate.manager',        'Gestión operativa: clientes, contratos, facturas y reportes',      1),
        ('Corporate Operations',     'corporate.operations',     'Operaciones diarias: clientes y contratos (lectura y creación)',   1),
        ('Corporate Reports Only',   'corporate.reports_only',   'Solo consulta de datos y exportación de reportes',                1),
        ('Corporate Settings Admin', 'corporate.settings_admin', 'Administración de pricing, marketplaces y ASINs',                 1)
    ) AS v (RoleName, RoleKey, Description, IsSystemRole)
)
INSERT INTO [Global].[Roles]
    (ApplicationId, RoleName, RoleKey, Description, IsSystemRole, IsActive, CreatedAt)
SELECT @CorporateAppId, r.RoleName, r.RoleKey, r.Description, r.IsSystemRole, 1, @Now
FROM RolesToInsert r
WHERE NOT EXISTS (
    SELECT 1 FROM [Global].[Roles] x WHERE x.RoleKey = r.RoleKey
);

PRINT 'Roles procesados.';

-- =============================================================================
-- 5. ROLE PERMISSIONS
-- Asignar todos los permisos activos al rol corporate.super_admin
-- Los demás roles quedan sin permisos asignados para configurarlos
-- manualmente desde el panel RBAC (o agregar aquí en iteraciones futuras)
-- =============================================================================

PRINT '';
PRINT '=== Asignando permisos a corporate.super_admin ===';

DECLARE @SuperAdminRoleId INT;
SELECT @SuperAdminRoleId = RoleId
FROM [Global].[Roles]
WHERE RoleKey = 'corporate.super_admin';

INSERT INTO [Global].[RolePermissions] (RoleId, PermissionId, GrantedBy, GrantedAt)
SELECT
    @SuperAdminRoleId,
    p.PermissionId,
    NULL,
    @Now
FROM [Global].[Permissions] p
WHERE p.IsActive = 1
  AND p.PermissionKey LIKE 'corporate.%'
  AND NOT EXISTS (
    SELECT 1
    FROM [Global].[RolePermissions] rp
    WHERE rp.RoleId = @SuperAdminRoleId
      AND rp.PermissionId = p.PermissionId
  );

DECLARE @PermCount INT = @@ROWCOUNT;
PRINT 'Permisos asignados a super_admin: ' + CAST(@PermCount AS VARCHAR);

-- =============================================================================
-- 6. RESUMEN FINAL
-- =============================================================================

PRINT '';
PRINT '=== Resumen de objetos en BD ===';

SELECT 'Resources activos Corporate'  AS Objeto, COUNT(*) AS Total
FROM [Global].[Resources]
WHERE ApplicationId = @CorporateAppId AND IsActive = 1
UNION ALL
SELECT 'Resources planned Corporate', COUNT(*)
FROM [Global].[Resources]
WHERE ApplicationId = @CorporateAppId AND IsActive = 0
UNION ALL
SELECT 'Actions totales', COUNT(*)
FROM [Global].[Actions]
WHERE IsActive = 1
UNION ALL
SELECT 'Permissions activos Corporate', COUNT(*)
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @CorporateAppId AND p.IsActive = 1
UNION ALL
SELECT 'Permissions planned Corporate', COUNT(*)
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @CorporateAppId AND p.IsActive = 0
UNION ALL
SELECT 'Roles Corporate', COUNT(*)
FROM [Global].[Roles]
WHERE ApplicationId = @CorporateAppId AND IsActive = 1
UNION ALL
SELECT 'Permisos de super_admin', COUNT(*)
FROM [Global].[RolePermissions] rp
INNER JOIN [Global].[Roles] ro ON ro.RoleId = rp.RoleId
WHERE ro.RoleKey = 'corporate.super_admin';

COMMIT TRANSACTION;
PRINT '';
PRINT '=== Script completado exitosamente ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    PRINT 'Línea: ' + CAST(ERROR_LINE() AS VARCHAR);
    THROW;
END CATCH;
