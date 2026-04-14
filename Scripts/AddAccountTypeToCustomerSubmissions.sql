-- Migration: Add AccountType column to Ecommerce.CustomerSubmissions
-- Date: 2026-04-13
-- Description: Adds AccountType field to store the platform account type
--              (e.g. Seller Central 3P, Vendor Central 1P) submitted via contact form.

IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'Ecommerce'
      AND TABLE_NAME   = 'CustomerSubmissions'
      AND COLUMN_NAME  = 'AccountType'
)
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD [AccountType] NVARCHAR(50) NULL;

    PRINT 'Column AccountType added to Ecommerce.CustomerSubmissions.';
END
ELSE
BEGIN
    PRINT 'Column AccountType already exists in Ecommerce.CustomerSubmissions. No changes made.';
END
