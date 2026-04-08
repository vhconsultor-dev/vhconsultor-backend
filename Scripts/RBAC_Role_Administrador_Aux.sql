/*
  Rol de ejemplo: Administrador Aux (Corporate)
  Idempotente. Ejecutar contra VH-DB (schema Global).

  Tras ejecutar:
  - Asignar permisos al rol vía UI (Settings > Roles > Role permissions) o insertar en RolePermissions.
  - Asignar el rol a usuarios vía UserRoles.

  Requiere que exista la aplicación Corporate en [Global].[Applications] (ApplicationKey acorde a vuestro entorno).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @CorporateAppId INT;
SELECT @CorporateAppId = ApplicationId
FROM [Global].[Applications]
WHERE ApplicationKey = 'corporate' OR ApplicationName LIKE '%Corporate%';
-- Si usáis otro ApplicationKey, ajustad la consulta anterior.

IF @CorporateAppId IS NULL
BEGIN
  RAISERROR('No se encontró ApplicationId para Corporate en [Global].[Applications]. Ajusta el script.', 16, 1);
  ROLLBACK TRANSACTION;
  RETURN;
END

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM [Global].[Roles] WHERE RoleKey = N'corporate.administrador_aux')
BEGIN
  INSERT INTO [Global].[Roles]
    (ApplicationId, RoleName, RoleKey, Description, IsSystemRole, IsActive, CreatedAt)
  VALUES
    (@CorporateAppId,
     N'Administrador Aux',
     N'corporate.administrador_aux',
     N'Perfil auxiliar de administración Corporate (permisos vía RolePermissions).',
     0,
     1,
     @Now);
  PRINT 'Rol corporate.administrador_aux creado.';
END
ELSE
  PRINT 'Rol corporate.administrador_aux ya existía; sin cambios.';

COMMIT TRANSACTION;

PRINT 'Listo. Asignad PermissionIds en [Global].[RolePermissions] y usuarios en [Global].[UserRoles].';
