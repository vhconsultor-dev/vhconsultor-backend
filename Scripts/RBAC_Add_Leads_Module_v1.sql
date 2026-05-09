/*
  RBAC – Add Leads Module to Corporate
  =====================================
  NOTA (v1): El módulo Leads ya está integrado en el seed principal
    RBAC_Seed_Corporate_Permissions_Align_Front_v1.sql (desde su actualización).
    Este script se mantiene por compatibilidad para entornos que ya tienen
    el seed principal aplicado sin Leads. Es idempotente (no duplica nada).

  Fuentes de verdad en código:
    - src/config/features.ts   → featureKey: 'corporate.leads'
    - src/config/permissions.ts → PERMISSIONS.LEADS

  Qué hace este script (idempotente):
    1) Inserta el Resource 'corporate.leads' si no existe.
    2) Asegura Actions necesarias (page.view, read, create, update, delete, export).
    3) Inserta Permissions para el Resource × Actions (si no existen).
    4) Asigna los nuevos permisos al rol SuperAdmin Corporate.

  Ejecución: SSMS / sqlcmd contra VH-DB.
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
  RAISERROR(N'ApplicationId for "corporate" not found in [Global].[Applications].', 16, 1);
  ROLLBACK TRANSACTION;
  RETURN;
END

PRINT N'ApplicationId Corporate = ' + CAST(@CorporateAppId AS NVARCHAR(20));

/* ========================================================================== */
/* 1) Resource: corporate.leads                                                */
/* ========================================================================== */
DECLARE @ResourceId INT;

SELECT @ResourceId = ResourceId
FROM [Global].[Resources]
WHERE ResourceKey = N'corporate.leads' AND ApplicationId = @CorporateAppId;

IF @ResourceId IS NULL
BEGIN
  INSERT INTO [Global].[Resources]
    (ApplicationId, ResourceName, ResourceKey, Description, Module, IsActive, CreatedAt)
  VALUES
    (@CorporateAppId, N'Leads', N'corporate.leads', N'Lead management (contact form submissions + manually created leads)', N'leads', 1, @Now);

  SET @ResourceId = SCOPE_IDENTITY();
  PRINT N'Resource "corporate.leads" inserted. ResourceId = ' + CAST(@ResourceId AS NVARCHAR(20));
END
ELSE
  PRINT N'Resource "corporate.leads" already exists. ResourceId = ' + CAST(@ResourceId AS NVARCHAR(20));

/* ========================================================================== */
/* 2) Ensure required Actions                                                  */
/* ========================================================================== */
DECLARE @Actions TABLE (ActionName NVARCHAR(100), ActionKey NVARCHAR(50), Description NVARCHAR(500));

INSERT INTO @Actions VALUES
(N'Page view', N'page.view', N'Access to the leads page'),
(N'Read',      N'read',      N'List and view leads'),
(N'Create',    N'create',    N'Create leads manually'),
(N'Update',    N'update',    N'Edit leads, mark as read, update notes'),
(N'Delete',    N'delete',    N'Delete leads'),
(N'Export',    N'export',    N'Export leads to Excel/CSV');

INSERT INTO [Global].[Actions] (ActionName, ActionKey, Description, IsActive, CreatedAt)
SELECT a.ActionName, a.ActionKey, a.Description, 1, @Now
FROM @Actions a
WHERE NOT EXISTS (SELECT 1 FROM [Global].[Actions] x WHERE x.ActionKey = a.ActionKey);

PRINT N'Actions ensured.';

/* ========================================================================== */
/* 3) Permissions: corporate.leads × each action                              */
/* ========================================================================== */
DECLARE @PermSpec TABLE (
  ActionKey      NVARCHAR(50)  NOT NULL,
  PermissionKey  NVARCHAR(100) NOT NULL,
  PermissionName NVARCHAR(200) NOT NULL
);

INSERT INTO @PermSpec VALUES
(N'page.view', N'corporate.leads.page.view', N'Leads · Page view'),
(N'read',      N'corporate.leads.read',      N'Leads · Read'),
(N'create',    N'corporate.leads.create',    N'Leads · Create'),
(N'update',    N'corporate.leads.update',    N'Leads · Update'),
(N'delete',    N'corporate.leads.delete',    N'Leads · Delete'),
(N'export',    N'corporate.leads.export',    N'Leads · Export');

INSERT INTO [Global].[Permissions]
  (ApplicationId, ResourceId, ActionId, PermissionName, PermissionKey, Description, IsActive, CreatedAt)
SELECT
  @CorporateAppId,
  @ResourceId,
  a.ActionId,
  p.PermissionName,
  p.PermissionKey,
  NULL,
  1,
  @Now
FROM @PermSpec p
INNER JOIN [Global].[Actions] a ON a.ActionKey = p.ActionKey
WHERE NOT EXISTS (
  SELECT 1 FROM [Global].[Permissions] x
  WHERE x.PermissionKey = p.PermissionKey
);

DECLARE @NewPerms INT = @@ROWCOUNT;
PRINT N'New permissions inserted: ' + CAST(@NewPerms AS NVARCHAR(20));

/* ========================================================================== */
/* 4) Assign all Leads permissions to the Corporate SuperAdmin role            */
/* ========================================================================== */
DECLARE @SuperAdminRoleId INT;

SELECT @SuperAdminRoleId = RoleId
FROM [Global].[Roles]
WHERE RoleKey = N'corporate.superadmin' AND ApplicationId = @CorporateAppId;

IF @SuperAdminRoleId IS NULL
BEGIN
  PRINT N'WARNING: Role "corporate.superadmin" not found. Skipping role-permission assignment.';
  PRINT N'Run RBAC_Seed_Corporate_Permissions_Align_Front_v1.sql first, then re-run this script.';
END
ELSE
BEGIN
  INSERT INTO [Global].[RolePermissions] (RoleId, PermissionId, GrantedBy, GrantedAt)
  SELECT @SuperAdminRoleId, p.PermissionId, NULL, @Now
  FROM [Global].[Permissions] p
  WHERE p.ResourceId = @ResourceId
    AND NOT EXISTS (
      SELECT 1 FROM [Global].[RolePermissions] rp
      WHERE rp.RoleId = @SuperAdminRoleId AND rp.PermissionId = p.PermissionId
    );

  PRINT N'Permissions assigned to SuperAdmin: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
END

COMMIT TRANSACTION;

PRINT N'';
PRINT N'=============================================================';
PRINT N'RBAC_Add_Leads_Module_v1 completed successfully.';
PRINT N'  New permissions : ' + CAST(@NewPerms AS NVARCHAR(20));
PRINT N'=============================================================';
PRINT N'';
PRINT N'Quick verification:';
PRINT N'  SELECT * FROM [Global].[Resources]   WHERE ResourceKey = ''corporate.leads'';';
PRINT N'  SELECT * FROM [Global].[Permissions] WHERE ResourceId = ' + CAST(@ResourceId AS NVARCHAR(20));
