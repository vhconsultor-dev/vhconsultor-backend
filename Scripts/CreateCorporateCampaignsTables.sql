-- =============================================================================
-- Script: Corporate Email Campaigns
-- Schema: Corporate
-- Idempotent
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'Corporate')
BEGIN
    EXEC(N'CREATE SCHEMA [Corporate]');
    PRINT N'Schema [Corporate] created.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'Campaigns'
)
BEGIN
    CREATE TABLE [Corporate].[Campaigns]
    (
        [CampaignId]        INT             NOT NULL IDENTITY(1,1),
        [Name]              NVARCHAR(200)   NOT NULL,
        [Subject]           NVARCHAR(500)   NOT NULL,
        [BodyContent]       NVARCHAR(MAX)   NOT NULL,
        [Status]            NVARCHAR(20)    NOT NULL CONSTRAINT [DF_Campaigns_Status] DEFAULT (N'Pending'),
        [ScheduledAt]       DATETIME2(7)    NULL,
        [SentAt]            DATETIME2(7)    NULL,
        [CreatedByUserId]   INT             NULL,
        [CreatedAt]         DATETIME2(7)    NOT NULL CONSTRAINT [DF_Campaigns_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]         DATETIME2(7)    NULL,

        CONSTRAINT [PK_Campaigns] PRIMARY KEY CLUSTERED ([CampaignId]),
        CONSTRAINT [CK_Campaigns_Status] CHECK ([Status] IN (N'Pending', N'Sent'))
    );

    CREATE NONCLUSTERED INDEX [IX_Campaigns_Status_CreatedAt]
        ON [Corporate].[Campaigns] ([Status], [CreatedAt] DESC);

    PRINT N'Table [Corporate].[Campaigns] created.';
END
ELSE
    PRINT N'[Corporate].[Campaigns] already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'CampaignAttachments'
)
BEGIN
    CREATE TABLE [Corporate].[CampaignAttachments]
    (
        [CampaignAttachmentId]  INT             NOT NULL IDENTITY(1,1),
        [CampaignId]            INT             NOT NULL,
        [FileUrl]               NVARCHAR(1000)  NOT NULL,
        [FileName]              NVARCHAR(255)   NOT NULL,
        [ContentType]           NVARCHAR(100)   NULL,
        [UploadedByUserId]      INT             NULL,
        [UploadedAt]            DATETIME2(7)    NOT NULL CONSTRAINT [DF_CampaignAttachments_UploadedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_CampaignAttachments] PRIMARY KEY CLUSTERED ([CampaignAttachmentId]),
        CONSTRAINT [FK_CampaignAttachments_Campaign] FOREIGN KEY ([CampaignId])
            REFERENCES [Corporate].[Campaigns] ([CampaignId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_CampaignAttachments_CampaignId]
        ON [Corporate].[CampaignAttachments] ([CampaignId]);

    PRINT N'Table [Corporate].[CampaignAttachments] created.';
END
ELSE
    PRINT N'[Corporate].[CampaignAttachments] already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = N'Corporate' AND t.name = N'CampaignProspects'
)
BEGIN
    CREATE TABLE [Corporate].[CampaignProspects]
    (
        [CampaignProspectId]    INT             NOT NULL IDENTITY(1,1),
        [CampaignId]            INT             NOT NULL,
        [Email]                 NVARCHAR(255)   NOT NULL,
        [LeadId]                INT             NULL,
        [FirstName]             NVARCHAR(100)   NULL,
        [LastName]              NVARCHAR(100)   NULL,
        [SentAt]                DATETIME2(7)    NULL,
        [CreatedAt]             DATETIME2(7)    NOT NULL CONSTRAINT [DF_CampaignProspects_CreatedAt] DEFAULT (GETUTCDATE()),

        CONSTRAINT [PK_CampaignProspects] PRIMARY KEY CLUSTERED ([CampaignProspectId]),
        CONSTRAINT [FK_CampaignProspects_Campaign] FOREIGN KEY ([CampaignId])
            REFERENCES [Corporate].[Campaigns] ([CampaignId]) ON DELETE CASCADE,
        CONSTRAINT [UQ_CampaignProspects_Campaign_Email] UNIQUE ([CampaignId], [Email])
    );

    CREATE NONCLUSTERED INDEX [IX_CampaignProspects_CampaignId]
        ON [Corporate].[CampaignProspects] ([CampaignId]);

    CREATE NONCLUSTERED INDEX [IX_CampaignProspects_LeadId]
        ON [Corporate].[CampaignProspects] ([LeadId])
        WHERE [LeadId] IS NOT NULL;

    PRINT N'Table [Corporate].[CampaignProspects] created.';
END
ELSE
    PRINT N'[Corporate].[CampaignProspects] already exists.';
GO

PRINT N'Corporate campaigns script completed.';
GO
