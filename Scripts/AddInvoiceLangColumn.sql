-- =============================================
-- Script para agregar columna Lang a Invoices
-- VHConsultor Backend - Idioma para PDF (CraftMyPDF)
-- Valores: 'en' (English), 'es' (Spanish)
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'Corporate' AND t.name = 'Invoices' AND c.name = 'Lang'
)
BEGIN
    ALTER TABLE [Corporate].[Invoices]
    ADD [Lang] NVARCHAR(10) NULL;
    
    PRINT 'Column Lang added to Corporate.Invoices';
END
ELSE
BEGIN
    PRINT 'Column Lang already exists in Corporate.Invoices';
END
GO
