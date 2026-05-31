-- =============================================================================
-- Script: Módulo de Oportunidades (Corporate) — tablas fases 1 a 5
-- Esquemas: Corporate, Ecommerce (columna en lead), Corporate.Contracts (columna)
-- Idempotente: puede ejecutarse varias veces sin duplicar objetos ni datos seed.
--
-- Fases cubiertas:
--   1  Opportunities + ConvertedToOpportunityId en leads
--   2  OpportunityStages (catálogo), FollowUps + adjuntos
--   3  OpportunityLostReasons (catálogo), Won/Lost en Opportunities, OpportunityId en Contracts
--   4  Comments, adjuntos de comentarios, menciones @
--   5  (SignalR no requiere tablas; usa las de comentarios)
--
-- Hora por defecto: UTC — GETUTCDATE() (conversión a zona local: front / capa de presentación)
-- Ejecución: SSMS / sqlcmd contra VH-DB
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- =============================================================================
-- 0) Esquemas
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'Corporate')
BEGIN
    EXEC(N'CREATE SCHEMA [Corporate]');
    PRINT N'Esquema [Corporate] creado.';
END

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'Ecommerce')
BEGIN
    RAISERROR(N'El esquema [Ecommerce] no existe. Verifique la base de datos.', 16, 1);
    RETURN;
END
GO

-- =============================================================================
-- 1) Catálogo de etapas (Fase 2)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityStages'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityStages]
    (
        [OpportunityStageId]      INT             NOT NULL IDENTITY(1,1),
        [StageKey]                NVARCHAR(50)    NOT NULL,
        [DisplayName]             NVARCHAR(200)   NOT NULL,
        [SortOrder]               INT             NOT NULL CONSTRAINT [DF_OpportunityStages_SortOrder] DEFAULT (0),
        [DefaultChecklistText]    NVARCHAR(500)   NULL,
        [IsActive]                BIT             NOT NULL CONSTRAINT [DF_OpportunityStages_IsActive] DEFAULT (1),
        [CreatedAt]               DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityStages_CreatedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_OpportunityStages] PRIMARY KEY CLUSTERED ([OpportunityStageId]),
        CONSTRAINT [UQ_OpportunityStages_StageKey] UNIQUE ([StageKey])
    );

    CREATE NONCLUSTERED INDEX [IX_OpportunityStages_IsActive_SortOrder]
        ON [Corporate].[OpportunityStages] ([IsActive], [SortOrder]);

    PRINT N'Tabla [Corporate].[OpportunityStages] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityStages] ya existe.';
GO

-- Seed etapas (idempotente)
IF EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'Corporate' AND t.name = N'OpportunityStages')
BEGIN
    MERGE [Corporate].[OpportunityStages] AS tgt
    USING (VALUES
        (N'first_contact',       N'First contact',           10, N'Contact the client on the same day as conversion.'),
        (N'follow_up',           N'Follow-up',               20, N'Follow up per agreement with the client.'),
        (N'proposal_sent',       N'Proposal sent',           30, N'Send proposal and attach evidence (email, PDF, etc.).'),
        (N'proposal_follow_up',  N'Proposal follow-up',      40, N'Confirm receipt and address questions about the proposal.'),
        (N'negotiation',         N'Negotiation',             50, N'Document agreements, objections, and next steps.'),
        (N'formalization',       N'Formalization',           60, N'Prepare close: contract, final terms, and signature.')
    ) AS src ([StageKey], [DisplayName], [SortOrder], [DefaultChecklistText])
    ON tgt.[StageKey] = src.[StageKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([StageKey], [DisplayName], [SortOrder], [DefaultChecklistText], [IsActive])
        VALUES (src.[StageKey], src.[DisplayName], src.[SortOrder], src.[DefaultChecklistText], 1)
    WHEN MATCHED THEN
        UPDATE SET
            [DisplayName] = src.[DisplayName],
            [SortOrder] = src.[SortOrder],
            [DefaultChecklistText] = src.[DefaultChecklistText];

    PRINT N'Seed [Corporate].[OpportunityStages] aplicado.';
END
GO

-- =============================================================================
-- 2) Catálogo motivos de pérdida (Fase 3)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityLostReasons'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityLostReasons]
    (
        [OpportunityLostReasonId] INT             NOT NULL IDENTITY(1,1),
        [ReasonKey]               NVARCHAR(50)    NOT NULL,
        [DisplayName]             NVARCHAR(200)   NOT NULL,
        [SortOrder]               INT             NOT NULL CONSTRAINT [DF_OpportunityLostReasons_SortOrder] DEFAULT (0),
        [IsActive]                BIT             NOT NULL CONSTRAINT [DF_OpportunityLostReasons_IsActive] DEFAULT (1),
        [CreatedAt]               DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityLostReasons_CreatedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_OpportunityLostReasons] PRIMARY KEY CLUSTERED ([OpportunityLostReasonId]),
        CONSTRAINT [UQ_OpportunityLostReasons_ReasonKey] UNIQUE ([ReasonKey])
    );

    PRINT N'Tabla [Corporate].[OpportunityLostReasons] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityLostReasons] ya existe.';
GO

IF EXISTS (SELECT 1 FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = N'Corporate' AND t.name = N'OpportunityLostReasons')
BEGIN
    MERGE [Corporate].[OpportunityLostReasons] AS tgt
    USING (VALUES
        (N'no_budget',        N'No budget',              10),
        (N'chose_competitor', N'Chose competitor',       20),
        (N'no_response',      N'No response',            30),
        (N'bad_timing',       N'Bad timing',             40),
        (N'not_a_fit',        N'Not a good fit',         50),
        (N'other',            N'Other',                  99)
    ) AS src ([ReasonKey], [DisplayName], [SortOrder])
    ON tgt.[ReasonKey] = src.[ReasonKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ReasonKey], [DisplayName], [SortOrder], [IsActive])
        VALUES (src.[ReasonKey], src.[DisplayName], src.[SortOrder], 1)
    WHEN MATCHED THEN
        UPDATE SET
            [DisplayName] = src.[DisplayName],
            [SortOrder] = src.[SortOrder];

    PRINT N'Seed [Corporate].[OpportunityLostReasons] aplicado.';
END
GO

-- =============================================================================
-- 3) Oportunidades — cabecera (Fase 1 + campos cierre Fase 3)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'Opportunities'
)
BEGIN
    CREATE TABLE [Corporate].[Opportunities]
    (
        [OpportunityId]             INT             NOT NULL IDENTITY(1,1),
        [SubmissionId]              INT             NULL,
        [Status]                    NVARCHAR(20)    NOT NULL CONSTRAINT [DF_Opportunities_Status] DEFAULT (N'Open'),
        [CurrentStageKey]           NVARCHAR(50)    NULL,
        [Title]                     NVARCHAR(255)   NOT NULL,

        -- Datos desnormalizados del lead / contacto
        [FirstName]                 NVARCHAR(100)   NOT NULL,
        [LastName]                  NVARCHAR(100)   NOT NULL,
        [Email]                     NVARCHAR(255)   NOT NULL,
        [PhoneNumber]               NVARCHAR(50)    NOT NULL,
        [Country]                   NVARCHAR(100)   NOT NULL,
        [BrandName]                 NVARCHAR(255)   NOT NULL,
        [NumberOfListings]          INT             NOT NULL CONSTRAINT [DF_Opportunities_NumberOfListings] DEFAULT (0),
        [ProductPageLink]           NVARCHAR(MAX)   NULL,
        [StoreLink]                 NVARCHAR(MAX)   NULL,
        [SelectedPlatform]          NVARCHAR(50)    NULL,
        [AccountType]               NVARCHAR(50)    NULL,
        [ServiceType]               NVARCHAR(50)    NULL,
        [AnnualSalesRange]          NVARCHAR(100)   NULL,
        [AdvertisingBudgetRange]    NVARCHAR(100)   NULL,
        [PromotionalBudgetRange]    NVARCHAR(100)   NULL,
        [AdditionalDetails]         NVARCHAR(MAX)   NULL,

        -- Asignación
        [AssignedToUserId]          INT             NOT NULL,
        [ViewerUserId]              INT             NULL,

        -- Cierre ganada (Fase 3)
        [CustomerId]                INT             NULL,
        [ContractId]                INT             NULL,
        [WonAt]                     DATETIME2(7)    NULL,
        [WonByUserId]               INT             NULL,

        -- Cierre perdida (Fase 3)
        [LostAt]                    DATETIME2(7)    NULL,
        [LostByUserId]              INT             NULL,
        [LostReasonKey]             NVARCHAR(50)    NULL,
        [LostReasonNotes]           NVARCHAR(500)   NULL,

        -- Conversión lead → oportunidad
        [ConvertedAt]               DATETIME2(7)    NOT NULL CONSTRAINT [DF_Opportunities_ConvertedAt] DEFAULT (GETUTCDATE()),
        [ConvertedByUserId]         INT             NULL,

        [CreatedAt]                 DATETIME2(7)    NOT NULL CONSTRAINT [DF_Opportunities_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]                 DATETIME2(7)    NULL,

        CONSTRAINT [PK_Opportunities] PRIMARY KEY CLUSTERED ([OpportunityId]),
        CONSTRAINT [CK_Opportunities_Status] CHECK ([Status] IN (N'Open', N'Won', N'Lost')),
        CONSTRAINT [FK_Opportunities_Submission] FOREIGN KEY ([SubmissionId])
            REFERENCES [Ecommerce].[CustomerSubmissions] ([SubmissionID]),
        CONSTRAINT [FK_Opportunities_AssignedToUser] FOREIGN KEY ([AssignedToUserId])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [FK_Opportunities_ViewerUser] FOREIGN KEY ([ViewerUserId])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [FK_Opportunities_ConvertedByUser] FOREIGN KEY ([ConvertedByUserId])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [FK_Opportunities_WonByUser] FOREIGN KEY ([WonByUserId])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [FK_Opportunities_LostByUser] FOREIGN KEY ([LostByUserId])
            REFERENCES [Global].[Users] ([UserId]),
        CONSTRAINT [FK_Opportunities_Customer] FOREIGN KEY ([CustomerId])
            REFERENCES [Corporate].[Customers] ([CustomerId]),
        CONSTRAINT [FK_Opportunities_Contract] FOREIGN KEY ([ContractId])
            REFERENCES [Corporate].[Contracts] ([ContractId]),
        CONSTRAINT [FK_Opportunities_CurrentStage] FOREIGN KEY ([CurrentStageKey])
            REFERENCES [Corporate].[OpportunityStages] ([StageKey]),
        CONSTRAINT [FK_Opportunities_LostReason] FOREIGN KEY ([LostReasonKey])
            REFERENCES [Corporate].[OpportunityLostReasons] ([ReasonKey])
    );

    CREATE NONCLUSTERED INDEX [IX_Opportunities_AssignedToUserId]
        ON [Corporate].[Opportunities] ([AssignedToUserId])
        INCLUDE ([Status], [CurrentStageKey], [Title], [BrandName], [ConvertedAt]);

    CREATE NONCLUSTERED INDEX [IX_Opportunities_ViewerUserId]
        ON [Corporate].[Opportunities] ([ViewerUserId])
        WHERE [ViewerUserId] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_Opportunities_Status]
        ON [Corporate].[Opportunities] ([Status], [ConvertedAt] DESC);

    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Opportunities_SubmissionId]
        ON [Corporate].[Opportunities] ([SubmissionId])
        WHERE [SubmissionId] IS NOT NULL;

    PRINT N'Tabla [Corporate].[Opportunities] creada.';
END
ELSE
    PRINT N'[Corporate].[Opportunities] ya existe.';
GO

-- =============================================================================
-- 4) Lead: marcar conversión (Fase 1)
-- =============================================================================
IF EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Ecommerce' AND t.name = N'CustomerSubmissions'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'Ecommerce'
          AND TABLE_NAME   = N'CustomerSubmissions'
          AND COLUMN_NAME  = N'ConvertedToOpportunityId'
    )
    BEGIN
        ALTER TABLE [Ecommerce].[CustomerSubmissions]
        ADD [ConvertedToOpportunityId] INT NULL;

        PRINT N'Columna [ConvertedToOpportunityId] agregada a [Ecommerce].[CustomerSubmissions].';
    END
    ELSE
        PRINT N'Columna [ConvertedToOpportunityId] ya existe en [Ecommerce].[CustomerSubmissions].';
END
ELSE
    PRINT N'ADVERTENCIA: [Ecommerce].[CustomerSubmissions] no existe; omitiendo columna ConvertedToOpportunityId.';
GO

-- FK lead → oportunidad (después de que ambas tablas/columnas existan)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Ecommerce].[CustomerSubmissions]') AND name = N'ConvertedToOpportunityId')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Corporate].[Opportunities]'))
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CustomerSubmissions_ConvertedOpportunity')
BEGIN
    ALTER TABLE [Ecommerce].[CustomerSubmissions]
    ADD CONSTRAINT [FK_CustomerSubmissions_ConvertedOpportunity]
        FOREIGN KEY ([ConvertedToOpportunityId])
        REFERENCES [Corporate].[Opportunities] ([OpportunityId]);

    PRINT N'FK [FK_CustomerSubmissions_ConvertedOpportunity] creada.';
END
GO

-- =============================================================================
-- 5) Seguimientos por etapa + adjuntos (Fase 2)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityFollowUps'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityFollowUps]
    (
        [FollowUpId]            INT             NOT NULL IDENTITY(1,1),
        [OpportunityId]         INT             NOT NULL,
        [StageKey]              NVARCHAR(50)    NOT NULL,
        [Status]                NVARCHAR(20)    NOT NULL CONSTRAINT [DF_OpportunityFollowUps_Status] DEFAULT (N'Pending'),
        [DueAt]                 DATETIME2(7)    NULL,
        [CompletedAt]           DATETIME2(7)    NULL,
        [CompletedByUserId]     INT             NULL,
        [Notes]                 NVARCHAR(MAX)   NULL,
        [IsRequired]            BIT             NOT NULL CONSTRAINT [DF_OpportunityFollowUps_IsRequired] DEFAULT (0),
        [CreatedAt]             DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityFollowUps_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]             DATETIME2(7)    NULL,

        CONSTRAINT [PK_OpportunityFollowUps] PRIMARY KEY CLUSTERED ([FollowUpId]),
        CONSTRAINT [CK_OpportunityFollowUps_Status] CHECK ([Status] IN (N'Pending', N'Completed', N'Cancelled')),
        CONSTRAINT [FK_OpportunityFollowUps_Opportunity] FOREIGN KEY ([OpportunityId])
            REFERENCES [Corporate].[Opportunities] ([OpportunityId])
            ON DELETE CASCADE,
        CONSTRAINT [FK_OpportunityFollowUps_Stage] FOREIGN KEY ([StageKey])
            REFERENCES [Corporate].[OpportunityStages] ([StageKey]),
        CONSTRAINT [FK_OpportunityFollowUps_CompletedByUser] FOREIGN KEY ([CompletedByUserId])
            REFERENCES [Global].[Users] ([UserId])
    );

    CREATE NONCLUSTERED INDEX [IX_OpportunityFollowUps_OpportunityId]
        ON [Corporate].[OpportunityFollowUps] ([OpportunityId], [CreatedAt] DESC);

    CREATE NONCLUSTERED INDEX [IX_OpportunityFollowUps_Pending_Alerts]
        ON [Corporate].[OpportunityFollowUps] ([Status], [DueAt])
        INCLUDE ([OpportunityId], [StageKey], [IsRequired])
        WHERE [Status] = N'Pending';

    PRINT N'Tabla [Corporate].[OpportunityFollowUps] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityFollowUps] ya existe.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityFollowUpAttachments'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityFollowUpAttachments]
    (
        [FollowUpAttachmentId]  INT             NOT NULL IDENTITY(1,1),
        [FollowUpId]            INT             NOT NULL,
        [FileUrl]               NVARCHAR(500)   NOT NULL,
        [FileName]              NVARCHAR(255)   NULL,
        [ContentType]           NVARCHAR(100)   NULL,
        [UploadedBy]            INT             NOT NULL,
        [UploadedAt]            DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityFollowUpAttachments_UploadedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_OpportunityFollowUpAttachments] PRIMARY KEY CLUSTERED ([FollowUpAttachmentId]),
        CONSTRAINT [FK_OpportunityFollowUpAttachments_FollowUp] FOREIGN KEY ([FollowUpId])
            REFERENCES [Corporate].[OpportunityFollowUps] ([FollowUpId])
            ON DELETE CASCADE,
        CONSTRAINT [FK_OpportunityFollowUpAttachments_UploadedBy] FOREIGN KEY ([UploadedBy])
            REFERENCES [Global].[Users] ([UserId])
    );

    CREATE NONCLUSTERED INDEX [IX_OpportunityFollowUpAttachments_FollowUpId]
        ON [Corporate].[OpportunityFollowUpAttachments] ([FollowUpId]);

    PRINT N'Tabla [Corporate].[OpportunityFollowUpAttachments] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityFollowUpAttachments] ya existe.';
GO

-- =============================================================================
-- 6) Comentarios + adjuntos + menciones @ (Fase 4)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityComments'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityComments]
    (
        [CommentId]             INT             NOT NULL IDENTITY(1,1),
        [OpportunityId]         INT             NOT NULL,
        [AuthorUserId]          INT             NOT NULL,
        [Body]                  NVARCHAR(MAX)   NOT NULL,
        [CreatedAt]             DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityComments_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]             DATETIME2(7)    NULL,

        CONSTRAINT [PK_OpportunityComments] PRIMARY KEY CLUSTERED ([CommentId]),
        CONSTRAINT [FK_OpportunityComments_Opportunity] FOREIGN KEY ([OpportunityId])
            REFERENCES [Corporate].[Opportunities] ([OpportunityId])
            ON DELETE CASCADE,
        CONSTRAINT [FK_OpportunityComments_AuthorUser] FOREIGN KEY ([AuthorUserId])
            REFERENCES [Global].[Users] ([UserId])
    );

    CREATE NONCLUSTERED INDEX [IX_OpportunityComments_OpportunityId_CreatedAt]
        ON [Corporate].[OpportunityComments] ([OpportunityId], [CreatedAt] DESC);

    PRINT N'Tabla [Corporate].[OpportunityComments] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityComments] ya existe.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityCommentAttachments'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityCommentAttachments]
    (
        [CommentAttachmentId]   INT             NOT NULL IDENTITY(1,1),
        [CommentId]             INT             NOT NULL,
        [FileUrl]               NVARCHAR(500)   NOT NULL,
        [FileName]              NVARCHAR(255)   NULL,
        [ContentType]           NVARCHAR(100)   NULL,
        [UploadedBy]            INT             NOT NULL,
        [UploadedAt]            DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityCommentAttachments_UploadedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_OpportunityCommentAttachments] PRIMARY KEY CLUSTERED ([CommentAttachmentId]),
        CONSTRAINT [FK_OpportunityCommentAttachments_Comment] FOREIGN KEY ([CommentId])
            REFERENCES [Corporate].[OpportunityComments] ([CommentId])
            ON DELETE CASCADE,
        CONSTRAINT [FK_OpportunityCommentAttachments_UploadedBy] FOREIGN KEY ([UploadedBy])
            REFERENCES [Global].[Users] ([UserId])
    );

    CREATE NONCLUSTERED INDEX [IX_OpportunityCommentAttachments_CommentId]
        ON [Corporate].[OpportunityCommentAttachments] ([CommentId]);

    PRINT N'Tabla [Corporate].[OpportunityCommentAttachments] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityCommentAttachments] ya existe.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'OpportunityCommentMentions'
)
BEGIN
    CREATE TABLE [Corporate].[OpportunityCommentMentions]
    (
        [CommentMentionId]      INT             NOT NULL IDENTITY(1,1),
        [CommentId]             INT             NOT NULL,
        [MentionedUserId]       INT             NOT NULL,
        [CreatedAt]             DATETIME2(7)    NOT NULL CONSTRAINT [DF_OpportunityCommentMentions_CreatedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_OpportunityCommentMentions] PRIMARY KEY CLUSTERED ([CommentMentionId]),
        CONSTRAINT [UQ_OpportunityCommentMentions_Comment_User] UNIQUE ([CommentId], [MentionedUserId]),
        CONSTRAINT [FK_OpportunityCommentMentions_Comment] FOREIGN KEY ([CommentId])
            REFERENCES [Corporate].[OpportunityComments] ([CommentId])
            ON DELETE CASCADE,
        CONSTRAINT [FK_OpportunityCommentMentions_MentionedUser] FOREIGN KEY ([MentionedUserId])
            REFERENCES [Global].[Users] ([UserId])
    );

    CREATE NONCLUSTERED INDEX [IX_OpportunityCommentMentions_MentionedUserId]
        ON [Corporate].[OpportunityCommentMentions] ([MentionedUserId], [CreatedAt] DESC);

    PRINT N'Tabla [Corporate].[OpportunityCommentMentions] creada.';
END
ELSE
    PRINT N'[Corporate].[OpportunityCommentMentions] ya existe.';
GO

-- =============================================================================
-- 7) Contratos: vínculo con oportunidad ganada (Fase 3)
-- =============================================================================
IF EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'Contracts'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'Corporate'
          AND TABLE_NAME   = N'Contracts'
          AND COLUMN_NAME  = N'OpportunityId'
    )
    BEGIN
        ALTER TABLE [Corporate].[Contracts]
        ADD [OpportunityId] INT NULL;

        PRINT N'Columna [OpportunityId] agregada a [Corporate].[Contracts].';
    END
    ELSE
        PRINT N'Columna [OpportunityId] ya existe en [Corporate].[Contracts].';
END
ELSE
    PRINT N'ADVERTENCIA: [Corporate].[Contracts] no existe; omitiendo columna OpportunityId.';
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Corporate].[Contracts]') AND name = N'OpportunityId')
   AND EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID(N'[Corporate].[Opportunities]'))
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Contracts_Opportunity')
BEGIN
    ALTER TABLE [Corporate].[Contracts]
    ADD CONSTRAINT [FK_Contracts_Opportunity]
        FOREIGN KEY ([OpportunityId])
        REFERENCES [Corporate].[Opportunities] ([OpportunityId]);

    PRINT N'FK [FK_Contracts_Opportunity] creada.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Corporate].[Contracts]') AND name = N'OpportunityId')
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'UQ_Contracts_OpportunityId'
         AND object_id = OBJECT_ID(N'[Corporate].[Contracts]')
   )
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_Contracts_OpportunityId]
        ON [Corporate].[Contracts] ([OpportunityId])
        WHERE [OpportunityId] IS NOT NULL;

    PRINT N'Índice [UQ_Contracts_OpportunityId] creado.';
END
GO

-- =============================================================================
-- Fin
-- =============================================================================
PRINT N'';
PRINT N'=============================================================';
PRINT N'CreateCorporateOpportunitiesTables completado.';
PRINT N'';
PRINT N'Tablas creadas / verificadas:';
PRINT N'  [Corporate].[OpportunityStages]';
PRINT N'  [Corporate].[OpportunityLostReasons]';
PRINT N'  [Corporate].[Opportunities]';
PRINT N'  [Corporate].[OpportunityFollowUps]';
PRINT N'  [Corporate].[OpportunityFollowUpAttachments]';
PRINT N'  [Corporate].[OpportunityComments]';
PRINT N'  [Corporate].[OpportunityCommentAttachments]';
PRINT N'  [Corporate].[OpportunityCommentMentions]';
PRINT N'Columnas:';
PRINT N'  [Ecommerce].[CustomerSubmissions].ConvertedToOpportunityId';
PRINT N'  [Corporate].[Contracts].OpportunityId';
PRINT N'=============================================================';
