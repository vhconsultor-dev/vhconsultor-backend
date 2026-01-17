-- Script para crear la tabla AmazonTokens
-- Schema: Amazon
-- Tabla: AmazonTokens

-- Crear el schema si no existe
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Amazon')
BEGIN
    EXEC('CREATE SCHEMA Amazon')
END
GO

-- Crear la tabla AmazonTokens
IF NOT EXISTS (SELECT * FROM sys.tables t 
               JOIN sys.schemas s ON t.schema_id = s.schema_id 
               WHERE s.name = 'Amazon' AND t.name = 'AmazonTokens')
BEGIN
    CREATE TABLE [Amazon].[AmazonTokens] (
        [TokenId] INT IDENTITY(1,1) NOT NULL,
        [RefreshToken] NVARCHAR(1000) NOT NULL,
        [AccessToken] NVARCHAR(2000) NOT NULL,
        [TokenType] NVARCHAR(50) NOT NULL DEFAULT 'bearer',
        [ExpiresIn] INT NOT NULL DEFAULT 3600,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT DATEADD(hour, -6, GETUTCDATE()),
        [ExpiresAt] DATETIME2 NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [ClientId] NVARCHAR(255) NULL,
        [Notes] NVARCHAR(500) NULL,
        
        CONSTRAINT [PK_AmazonTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC)
    )
END
GO

-- Crear índices para mejorar el rendimiento
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AmazonTokens_IsActive_ExpiresAt' 
               AND object_id = OBJECT_ID('Amazon.AmazonTokens'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AmazonTokens_IsActive_ExpiresAt]
    ON [Amazon].[AmazonTokens] ([IsActive], [ExpiresAt])
    INCLUDE ([AccessToken], [TokenType], [ExpiresIn])
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AmazonTokens_ClientId' 
               AND object_id = OBJECT_ID('Amazon.AmazonTokens'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AmazonTokens_ClientId]
    ON [Amazon].[AmazonTokens] ([ClientId])
    WHERE [ClientId] IS NOT NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AmazonTokens_CreatedAt' 
               AND object_id = OBJECT_ID('Amazon.AmazonTokens'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AmazonTokens_CreatedAt]
    ON [Amazon].[AmazonTokens] ([CreatedAt] DESC)
END
GO

PRINT 'Tabla Amazon.AmazonTokens creada exitosamente con sus índices'
GO
