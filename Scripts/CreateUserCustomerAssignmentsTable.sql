-- =============================================================================
-- Tabla: Global.UserCustomerAssignments
-- Vincula usuarios corporate (Global.Users) con clientes (Corporate.Customers)
-- que pueden atender. Las cuentas Amazon se obtienen por CustomerId.
-- =============================================================================

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Global')
BEGIN
    RAISERROR(N'El esquema Global no existe.', 16, 1);
    RETURN;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Global' AND t.name = N'UserCustomerAssignments'
)
BEGIN
    CREATE TABLE [Global].[UserCustomerAssignments]
    (
        [UserCustomerAssignmentId] INT             NOT NULL IDENTITY(1,1),
        [UserId]                     INT             NOT NULL,
        [CustomerId]                 INT             NOT NULL,
        [IsActive]                   BIT             NOT NULL CONSTRAINT [DF_UserCustomerAssignments_IsActive] DEFAULT (1),
        [AssignedAt]                 DATETIME2(7)    NOT NULL CONSTRAINT [DF_UserCustomerAssignments_AssignedAt] DEFAULT (GETUTCDATE()),
        [AssignedBy]                 INT             NULL,
        [UpdatedAt]                  DATETIME2(7)    NULL,

        CONSTRAINT [PK_UserCustomerAssignments] PRIMARY KEY CLUSTERED ([UserCustomerAssignmentId]),
        CONSTRAINT [FK_UserCustomerAssignments_Users] FOREIGN KEY ([UserId])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [FK_UserCustomerAssignments_Customers] FOREIGN KEY ([CustomerId])
            REFERENCES [Corporate].[Customers] ([CustomerId]),
        CONSTRAINT [FK_UserCustomerAssignments_AssignedByUser] FOREIGN KEY ([AssignedBy])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [UQ_UserCustomerAssignments_UserId_CustomerId] UNIQUE ([UserId], [CustomerId])
    );

    CREATE NONCLUSTERED INDEX [IX_UserCustomerAssignments_UserId]
        ON [Global].[UserCustomerAssignments] ([UserId])
        INCLUDE ([CustomerId], [IsActive]);

    CREATE NONCLUSTERED INDEX [IX_UserCustomerAssignments_CustomerId]
        ON [Global].[UserCustomerAssignments] ([CustomerId])
        INCLUDE ([UserId], [IsActive]);

    PRINT N'Tabla [Global].[UserCustomerAssignments] creada correctamente.';
END
ELSE
    PRINT N'La tabla [Global].[UserCustomerAssignments] ya existe.';
GO
