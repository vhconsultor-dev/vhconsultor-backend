-- Consulta para mostrar las credenciales de prueba disponibles
-- Base de datos: LD_OPERACION
-- Schema: AutoshopManagement

USE [LD_OPERACION]
GO

-- Consultar todas las credenciales disponibles para testing
SELECT 
    'Credenciales de prueba disponibles para JWT' AS [Información],
    CompanyId AS [ID_Empresa],
    Username AS [Usuario],
    Password AS [Contraseña],
    Profile AS [Perfil],
    SecureKey AS [LlaveSegura],
    IsActive AS [Activo],
    CreatedDate AS [FechaCreacion]
FROM [AutoshopManagement].[JwtCredentials]
WHERE IsActive = 1
ORDER BY CompanyId, Profile, Username;

GO

-- Mostrar resumen por empresa
SELECT 
    CompanyId AS [ID_Empresa],
    COUNT(*) AS [TotalUsuarios],
    STRING_AGG(Profile, ', ') AS [Perfiles]
FROM [AutoshopManagement].[JwtCredentials]
WHERE IsActive = 1
GROUP BY CompanyId
ORDER BY CompanyId;

GO

-- Ejemplos de uso para testing
PRINT '========================================='
PRINT 'CREDENCIALES DE PRUEBA PARA JWT TESTING'
PRINT '========================================='
PRINT ''
PRINT 'Empresa 2:'
PRINT '- Usuario: admin2 | Contraseña: Admin123! | Perfil: Administrator'
PRINT '- Usuario: supervisor2 | Contraseña: Supervisor123! | Perfil: Supervisor'
PRINT '- Usuario: advisor2 | Contraseña: Advisor123! | Perfil: Advisor'
PRINT '- Usuario: technician2 | Contraseña: Technician123! | Perfil: Technician'
PRINT ''
PRINT 'Empresa 3:'
PRINT '- Usuario: admin3 | Contraseña: Admin123! | Perfil: Administrator'
PRINT '- Usuario: supervisor3 | Contraseña: Supervisor123! | Perfil: Supervisor'
PRINT '- Usuario: advisor3 | Contraseña: Advisor123! | Perfil: Advisor'
PRINT '- Usuario: technician3 | Contraseña: Technician123! | Perfil: Technician'
PRINT ''
PRINT 'Empresa 5:'
PRINT '- Usuario: admin5 | Contraseña: Admin123! | Perfil: Administrator'
PRINT '- Usuario: supervisor5 | Contraseña: Supervisor123! | Perfil: Supervisor'
PRINT '- Usuario: advisor5 | Contraseña: Advisor123! | Perfil: Advisor'
PRINT '- Usuario: technician5 | Contraseña: Technician123! | Perfil: Technician'
PRINT ''
PRINT 'Ejemplo de JSON para POST /api/Security/login:'
PRINT '{'
PRINT '  "companyId": 2,'
PRINT '  "username": "admin2",'
PRINT '  "password": "Admin123!"'
PRINT '}'
PRINT '=========================================' 