-- Migration: Add Lead Management columns to Ecommerce.CustomerSubmissions
-- Date: 2026-04-14
-- Description: Adds fields to support the Leads module in Corporate:
--              IsRead, ReadAt, ReadByUserId, CreatedByUserId, Notes

SET NOCOUNT ON;

-- IsRead
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'Ecommerce' AND TABLE_NAME = 'CustomerSubmissions' AND COLUMN_NAME = 'IsRead'
)
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD [IsRead] BIT NOT NULL DEFAULT (0);
    PRINT 'Column IsRead added.';
END
ELSE PRINT 'Column IsRead already exists.';

-- ReadAt
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'Ecommerce' AND TABLE_NAME = 'CustomerSubmissions' AND COLUMN_NAME = 'ReadAt'
)
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD [ReadAt] DATETIME2 NULL;
    PRINT 'Column ReadAt added.';
END
ELSE PRINT 'Column ReadAt already exists.';

-- ReadByUserId
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'Ecommerce' AND TABLE_NAME = 'CustomerSubmissions' AND COLUMN_NAME = 'ReadByUserId'
)
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD [ReadByUserId] INT NULL;
    PRINT 'Column ReadByUserId added.';
END
ELSE PRINT 'Column ReadByUserId already exists.';

-- CreatedByUserId (NULL = vino del formulario público; NOT NULL = creado manualmente desde Corporate)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'Ecommerce' AND TABLE_NAME = 'CustomerSubmissions' AND COLUMN_NAME = 'CreatedByUserId'
)
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD [CreatedByUserId] INT NULL;
    PRINT 'Column CreatedByUserId added.';
END
ELSE PRINT 'Column CreatedByUserId already exists.';

-- Notes
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'Ecommerce' AND TABLE_NAME = 'CustomerSubmissions' AND COLUMN_NAME = 'Notes'
)
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD [Notes] NVARCHAR(MAX) NULL;
    PRINT 'Column Notes added.';
END
ELSE PRINT 'Column Notes already exists.';

PRINT '';
PRINT 'Migration AddLeadManagementColumnsToCustomerSubmissions completed.';
