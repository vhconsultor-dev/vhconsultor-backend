-- =============================================================================
-- Script: RBAC Cleanup - Desactivar objetos Corporate v1
-- Descripción: Desactiva Resources, Permissions y Roles del esquema anterior
--              (keys sin prefijo 'corporate.') que pertenecen a la app Corporate.
--
--              NO toca nada de BrandPartner ni Actions globales.
--              Solo desactiva (IsActive = 0), NO elimina registros.
--
-- Fecha: 2026-03-26
--
-- INSTRUCCIONES:
--   1. Ejecutar primero SOLO el bloque "PASO 0 - PREVIEW" para revisar qué se afecta.
--   2. Si el resultado es correcto, ejecutar el script completo.
-- =============================================================================

SET NOCOUNT ON;

DECLARE @CorporateAppId INT;

SELECT @CorporateAppId = ApplicationId
FROM [Global].[Applications]
WHERE ApplicationKey = 'corporate' AND IsActive = 1;

PRINT 'ApplicationId Corporate: ' + CAST(@CorporateAppId AS VARCHAR);

-- =============================================================================
-- PASO 0 — PREVIEW (ejecutar primero para revisar, no modifica nada)
-- =============================================================================

PRINT '';
PRINT '=== PREVIEW: Resources v1 que se desactivarán ===';
SELECT
    ResourceId,
    ResourceName,
    ResourceKey,
    Module,
    IsActive
FROM [Global].[Resources]
WHERE ApplicationId = @CorporateAppId
  AND ResourceKey NOT LIKE 'corporate.%'
  AND IsActive = 1
ORDER BY ResourceKey;

PRINT '';
PRINT '=== PREVIEW: Permissions v1 que se desactivarán ===';
SELECT
    p.PermissionId,
    p.PermissionKey,
    p.IsActive
FROM [Global].[Permissions] p
INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
WHERE r.ApplicationId = @CorporateAppId
  AND p.PermissionKey NOT LIKE 'corporate.%'
  AND p.IsActive = 1
ORDER BY p.PermissionKey;

PRINT '';
PRINT '=== PREVIEW: Roles v1 que se desactivarán ===';
SELECT
    RoleId,
    RoleName,
    RoleKey,
    IsActive
FROM [Global].[Roles]
WHERE ApplicationId = @CorporateAppId
  AND RoleKey NOT LIKE 'corporate.%'
  AND IsActive = 1
ORDER BY RoleKey;

PRINT '';
PRINT '=== PREVIEW: RolePermissions que se desactivarán (permisos v1 asignados a roles) ===';
SELECT
    rp.RolePermissionId,
    ro.RoleKey,
    p.PermissionKey
FROM [Global].[RolePermissions] rp
INNER JOIN [Global].[Roles]       ro ON ro.RoleId       = rp.RoleId
INNER JOIN [Global].[Permissions]  p  ON p.PermissionId  = rp.PermissionId
INNER JOIN [Global].[Resources]    r  ON r.ResourceId    = p.ResourceId
WHERE r.ApplicationId = @CorporateAppId
  AND p.PermissionKey NOT LIKE 'corporate.%'
ORDER BY ro.RoleKey, p.PermissionKey;

PRINT '';
PRINT '>>> Revisa los resultados de arriba antes de continuar con la limpieza. <<<';

-- =============================================================================
-- PASO 1 — LIMPIEZA (descomentar y ejecutar solo si el preview es correcto)
-- =============================================================================
/*
BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @UpdatedAt DATETIME = GETDATE();

    -- 1a. Desactivar RolePermissions ligados a permisos v1 de Corporate
    --     (evita que roles activos sigan teniendo permisos del esquema viejo)
    UPDATE rp
    SET rp.GrantedAt = rp.GrantedAt   -- sin columna IsActive en RolePermissions,
                                       -- los eliminamos directamente para limpiar
    FROM [Global].[RolePermissions] rp
    INNER JOIN [Global].[Permissions]  p  ON p.PermissionId  = rp.PermissionId
    INNER JOIN [Global].[Resources]    r  ON r.ResourceId    = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId
      AND p.PermissionKey NOT LIKE 'corporate.%';
    -- Nota: RolePermissions no tiene IsActive, los borramos en el paso real abajo.

    -- 1b. Eliminar RolePermissions de permisos v1 (tabla sin IsActive, se borra directamente)
    DELETE rp
    FROM [Global].[RolePermissions] rp
    INNER JOIN [Global].[Permissions]  p  ON p.PermissionId  = rp.PermissionId
    INNER JOIN [Global].[Resources]    r  ON r.ResourceId    = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId
      AND p.PermissionKey NOT LIKE 'corporate.%';

    PRINT 'RolePermissions v1 eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 1c. Eliminar UserPermissions directos de permisos v1 (si los hubiera)
    DELETE up
    FROM [Global].[UserPermissions] up
    INNER JOIN [Global].[Permissions]  p  ON p.PermissionId  = up.PermissionId
    INNER JOIN [Global].[Resources]    r  ON r.ResourceId    = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId
      AND p.PermissionKey NOT LIKE 'corporate.%';

    PRINT 'UserPermissions v1 eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 1d. Eliminar UserPermissionDenials de permisos v1 (si los hubiera)
    DELETE upd
    FROM [Global].[UserPermissionDenials] upd
    INNER JOIN [Global].[Permissions]  p  ON p.PermissionId  = upd.PermissionId
    INNER JOIN [Global].[Resources]    r  ON r.ResourceId    = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId
      AND p.PermissionKey NOT LIKE 'corporate.%';

    PRINT 'UserPermissionDenials v1 eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 2. Desactivar Permissions v1 de Corporate
    UPDATE [Global].[Permissions]
    SET IsActive = 0, UpdatedAt = @UpdatedAt
    FROM [Global].[Permissions] p
    INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId
      AND p.PermissionKey NOT LIKE 'corporate.%'
      AND p.IsActive = 1;

    PRINT 'Permissions v1 desactivados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 3. Desactivar Resources v1 de Corporate
    UPDATE [Global].[Resources]
    SET IsActive = 0, UpdatedAt = @UpdatedAt
    WHERE ApplicationId = @CorporateAppId
      AND ResourceKey NOT LIKE 'corporate.%'
      AND IsActive = 1;

    PRINT 'Resources v1 desactivados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 4. Desactivar Roles v1 de Corporate
    --    Primero quitar UserRoles asociados a roles v1 (desactivarlos)
    UPDATE [Global].[UserRoles]
    SET IsActive = 0
    FROM [Global].[UserRoles] ur
    INNER JOIN [Global].[Roles] ro ON ro.RoleId = ur.RoleId
    WHERE ro.ApplicationId = @CorporateAppId
      AND ro.RoleKey NOT LIKE 'corporate.%'
      AND ur.IsActive = 1;

    PRINT 'UserRoles v1 desactivados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    UPDATE [Global].[Roles]
    SET IsActive = 0, UpdatedAt = @UpdatedAt
    WHERE ApplicationId = @CorporateAppId
      AND RoleKey NOT LIKE 'corporate.%'
      AND IsActive = 1;

    PRINT 'Roles v1 desactivados: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- 5. Resumen final
    PRINT '';
    PRINT '=== Resumen post-limpieza ===';

    SELECT 'Resources activos Corporate'   AS Objeto, COUNT(*) AS Total
    FROM [Global].[Resources]
    WHERE ApplicationId = @CorporateAppId AND IsActive = 1
    UNION ALL
    SELECT 'Resources inactivos Corporate', COUNT(*)
    FROM [Global].[Resources]
    WHERE ApplicationId = @CorporateAppId AND IsActive = 0
    UNION ALL
    SELECT 'Permissions activos Corporate', COUNT(*)
    FROM [Global].[Permissions] p
    INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId AND p.IsActive = 1
    UNION ALL
    SELECT 'Permissions inactivos Corporate', COUNT(*)
    FROM [Global].[Permissions] p
    INNER JOIN [Global].[Resources] r ON r.ResourceId = p.ResourceId
    WHERE r.ApplicationId = @CorporateAppId AND p.IsActive = 0
    UNION ALL
    SELECT 'Roles activos Corporate', COUNT(*)
    FROM [Global].[Roles]
    WHERE ApplicationId = @CorporateAppId AND IsActive = 1
    UNION ALL
    SELECT 'Roles inactivos Corporate', COUNT(*)
    FROM [Global].[Roles]
    WHERE ApplicationId = @CorporateAppId AND IsActive = 0
    UNION ALL
    SELECT 'Permisos de super_admin', COUNT(*)
    FROM [Global].[RolePermissions] rp
    INNER JOIN [Global].[Roles] ro ON ro.RoleId = rp.RoleId
    WHERE ro.RoleKey = 'corporate.super_admin';

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '=== Limpieza completada exitosamente ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    PRINT 'Línea: ' + CAST(ERROR_LINE() AS VARCHAR);
    THROW;
END CATCH;
*/
