-- =============================================
-- Script para crear la tabla ErrorLogs
-- VHConsultor Backend - Sistema de Logging de Errores
-- =============================================

-- Verificar si la tabla ErrorLogs existe, si no existe la creamos
IF NOT EXISTS (SELECT * FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'dbo' AND t.name = 'ErrorLogs')
BEGIN
    CREATE TABLE [dbo].[ErrorLogs] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ErrorNumber] NVARCHAR(50) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [StackTrace] NVARCHAR(MAX) NULL,
        [Source] NVARCHAR(255) NULL,
        [RequestPath] NVARCHAR(500) NULL,
        [RequestMethod] NVARCHAR(10) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [UserId] NVARCHAR(100) NULL,
        [RequestBody] NVARCHAR(MAX) NULL,
        [QueryString] NVARCHAR(MAX) NULL,
        [ExceptionType] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),
        [Environment] NVARCHAR(50) NULL,
        [AdditionalData] NVARCHAR(MAX) NULL,
        
        CONSTRAINT [PK_ErrorLogs] PRIMARY KEY ([Id])
    );
    
    -- Crear índices para mejorar el rendimiento de consultas
    CREATE NONCLUSTERED INDEX [IX_ErrorLogs_CreatedAt] 
    ON [dbo].[ErrorLogs] ([CreatedAt] DESC);
    
    CREATE NONCLUSTERED INDEX [IX_ErrorLogs_ErrorNumber] 
    ON [dbo].[ErrorLogs] ([ErrorNumber]);
    
    CREATE NONCLUSTERED INDEX [IX_ErrorLogs_Environment] 
    ON [dbo].[ErrorLogs] ([Environment]);
    
    CREATE NONCLUSTERED INDEX [IX_ErrorLogs_ExceptionType] 
    ON [dbo].[ErrorLogs] ([ExceptionType]);
    
    -- Comentarios en la tabla
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Tabla para almacenar logs de errores del sistema VHConsultor Backend', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs';
    
    -- Comentarios en las columnas principales
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Identificador único del error', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs', 
        @level2type = N'COLUMN', @level2name = N'Id';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Número único del error generado automáticamente', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs', 
        @level2type = N'COLUMN', @level2name = N'ErrorNumber';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Mensaje descriptivo del error', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs', 
        @level2type = N'COLUMN', @level2name = N'Message';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Stack trace completo de la excepción', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs', 
        @level2type = N'COLUMN', @level2name = N'StackTrace';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Fecha y hora de creación del error en zona horaria de Costa Rica', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs', 
        @level2type = N'COLUMN', @level2name = N'CreatedAt';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Entorno donde ocurrió el error (Development, Production, etc.)', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'ErrorLogs', 
        @level2type = N'COLUMN', @level2name = N'Environment';
    
    PRINT 'Tabla [dbo].[ErrorLogs] creada exitosamente con índices y comentarios';
END
ELSE
BEGIN
    PRINT 'La tabla [dbo].[ErrorLogs] ya existe';
END

-- Verificar la estructura de la tabla creada
SELECT 
    COLUMN_NAME as 'Columna',
    DATA_TYPE as 'Tipo de Dato',
    CHARACTER_MAXIMUM_LENGTH as 'Longitud Máxima',
    IS_NULLABLE as 'Permite NULL',
    COLUMN_DEFAULT as 'Valor por Defecto'
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_NAME = 'ErrorLogs'
ORDER BY ORDINAL_POSITION;

-- Mostrar los índices creados
SELECT 
    i.name as 'Nombre del Índice',
    i.type_desc as 'Tipo',
    c.name as 'Columna'
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.ErrorLogs')
ORDER BY i.name, ic.key_ordinal;
