-- =============================================================================
-- Script: Crear esquema BrandPartner, tabla BrandPartnerUsers e historial de login
-- Descripción: Usuarios de Brand Partner (clientes) vinculados a Corporate.Customers.
--              Login siempre con correo (Email). Incluye obligación de cambiar
--              contraseña y tabla de historial de inicio de sesión.
-- =============================================================================

-- Crear esquema BrandPartner si no existe
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'BrandPartner')
BEGIN
    EXEC('CREATE SCHEMA BrandPartner');
END
GO

-- Tabla: BrandPartner.BrandPartnerUsers
-- Login siempre con Email (no username). Nombre y apellidos obligatorios.
-- RequirePasswordChangeOnNextLogin = 1 tras restablecer con contraseña temporal.
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'BrandPartner' AND t.name = 'BrandPartnerUsers')
BEGIN
    CREATE TABLE BrandPartner.BrandPartnerUsers
    (
        BrandPartnerUserId              INT             NOT NULL IDENTITY(1,1),
        CustomerId                      INT             NOT NULL,
        Email                           NVARCHAR(255)   NOT NULL,
        PasswordHash                    NVARCHAR(500)   NOT NULL,
        FirstName                       NVARCHAR(100)   NOT NULL,
        LastName                        NVARCHAR(100)   NOT NULL,
        PhoneNumber                     NVARCHAR(50)    NULL,
        IsActive                        BIT             NOT NULL CONSTRAINT DF_BrandPartnerUsers_IsActive DEFAULT 1,
        EmailVerified                   BIT             NOT NULL CONSTRAINT DF_BrandPartnerUsers_EmailVerified DEFAULT 0,
        RequirePasswordChangeOnNextLogin BIT           NOT NULL CONSTRAINT DF_BrandPartnerUsers_RequirePasswordChange DEFAULT 0,
        FailedLoginAttempts            INT             NOT NULL CONSTRAINT DF_BrandPartnerUsers_FailedLoginAttempts DEFAULT 0,
        LockedUntil                    DATETIME2(7)    NULL,
        LastLogin                      DATETIME2(7)    NULL,
        LastLoginIP                    NVARCHAR(45)    NULL,
        LastLoginUserAgent             NVARCHAR(500)   NULL,
        CreatedAt                      DATETIME2(7)    NOT NULL CONSTRAINT DF_BrandPartnerUsers_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt                      DATETIME2(7)    NULL,
        CreatedBy                      NVARCHAR(255)   NULL,

        CONSTRAINT PK_BrandPartnerUsers PRIMARY KEY CLUSTERED (BrandPartnerUserId),
        CONSTRAINT FK_BrandPartnerUsers_Customers FOREIGN KEY (CustomerId)
            REFERENCES Corporate.Customers (CustomerId),
        CONSTRAINT UQ_BrandPartnerUsers_CustomerId_Email UNIQUE (CustomerId, Email)
    );

    CREATE NONCLUSTERED INDEX IX_BrandPartnerUsers_CustomerId
        ON BrandPartner.BrandPartnerUsers (CustomerId);

    CREATE NONCLUSTERED INDEX IX_BrandPartnerUsers_Email
        ON BrandPartner.BrandPartnerUsers (Email) INCLUDE (CustomerId, PasswordHash, IsActive, LockedUntil, RequirePasswordChangeOnNextLogin);

    PRINT 'Tabla BrandPartner.BrandPartnerUsers creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.BrandPartnerUsers ya existe.';
END
GO

-- Tabla: BrandPartner.BrandPartnerUserLoginHistory
-- Historial de inicio de sesión (exitosos y fallidos) por usuario Brand Partner.
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'BrandPartner' AND t.name = 'BrandPartnerUserLoginHistory')
BEGIN
    CREATE TABLE BrandPartner.BrandPartnerUserLoginHistory
    (
        LoginHistoryId         INT             NOT NULL IDENTITY(1,1),
        BrandPartnerUserId     INT             NOT NULL,
        LoginDate              DATETIME2(7)    NOT NULL CONSTRAINT DF_BrandPartnerUserLoginHistory_LoginDate DEFAULT (GETUTCDATE()),
        IPAddress              NVARCHAR(45)    NOT NULL,
        UserAgent              NVARCHAR(500)   NULL,
        Location               NVARCHAR(255)   NULL,
        Country                NVARCHAR(100)   NULL,
        City                   NVARCHAR(100)   NULL,
        LoginSuccessful        BIT             NOT NULL,
        FailureReason          NVARCHAR(255)   NULL,
        SessionId              NVARCHAR(255)   NULL,

        CONSTRAINT PK_BrandPartnerUserLoginHistory PRIMARY KEY CLUSTERED (LoginHistoryId),
        CONSTRAINT FK_BrandPartnerUserLoginHistory_BrandPartnerUsers FOREIGN KEY (BrandPartnerUserId)
            REFERENCES BrandPartner.BrandPartnerUsers (BrandPartnerUserId)
    );

    CREATE NONCLUSTERED INDEX IX_BrandPartnerUserLoginHistory_BrandPartnerUserId
        ON BrandPartner.BrandPartnerUserLoginHistory (BrandPartnerUserId);

    CREATE NONCLUSTERED INDEX IX_BrandPartnerUserLoginHistory_LoginDate
        ON BrandPartner.BrandPartnerUserLoginHistory (LoginDate DESC);

    CREATE NONCLUSTERED INDEX IX_BrandPartnerUserLoginHistory_UserId_LoginDate
        ON BrandPartner.BrandPartnerUserLoginHistory (BrandPartnerUserId, LoginDate DESC);

    PRINT 'Tabla BrandPartner.BrandPartnerUserLoginHistory creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.BrandPartnerUserLoginHistory ya existe.';
END
GO

-- Tabla: BrandPartner.BrandPartnerTwoFactorCodes
-- Códigos de verificación 2FA: 1 letra mayúscula (A-Z) + 4 dígitos aleatorios
-- Expiran en 3 minutos desde CreatedAt
IF NOT EXISTS (SELECT * FROM sys.tables t
               JOIN sys.schemas s ON t.schema_id = s.schema_id
               WHERE s.name = 'BrandPartner' AND t.name = 'BrandPartnerTwoFactorCodes')
BEGIN
    CREATE TABLE BrandPartner.BrandPartnerTwoFactorCodes
    (
        TwoFactorCodeId     INT             NOT NULL IDENTITY(1,1),
        BrandPartnerUserId  INT             NOT NULL,
        Code                NVARCHAR(5)     NOT NULL,
        CreatedAt           DATETIME2(7)    NOT NULL CONSTRAINT DF_BrandPartnerTwoFactorCodes_CreatedAt DEFAULT (GETUTCDATE()),
        ExpiresAt           DATETIME2(7)    NOT NULL,
        IsUsed              BIT             NOT NULL CONSTRAINT DF_BrandPartnerTwoFactorCodes_IsUsed DEFAULT 0,
        UsedAt              DATETIME2(7)    NULL,

        CONSTRAINT PK_BrandPartnerTwoFactorCodes PRIMARY KEY CLUSTERED (TwoFactorCodeId),
        CONSTRAINT FK_BrandPartnerTwoFactorCodes_BrandPartnerUsers FOREIGN KEY (BrandPartnerUserId)
            REFERENCES BrandPartner.BrandPartnerUsers (BrandPartnerUserId)
    );

    CREATE NONCLUSTERED INDEX IX_BrandPartnerTwoFactorCodes_BrandPartnerUserId
        ON BrandPartner.BrandPartnerTwoFactorCodes (BrandPartnerUserId);

    CREATE NONCLUSTERED INDEX IX_BrandPartnerTwoFactorCodes_ExpiresAt
        ON BrandPartner.BrandPartnerTwoFactorCodes (ExpiresAt) WHERE IsUsed = 0;

    PRINT 'Tabla BrandPartner.BrandPartnerTwoFactorCodes creada correctamente.';
END
ELSE
BEGIN
    PRINT 'La tabla BrandPartner.BrandPartnerTwoFactorCodes ya existe.';
END
GO
