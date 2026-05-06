-- =============================================
-- Manual Invoice Tables (not tied to a contract)
-- Schema: Corporate
-- =============================================

-- 1. Headers table
CREATE TABLE [Corporate].[ManualInvoiceHeaders] (
    ManualInvoiceHeaderId   INT             IDENTITY(1,1)   NOT NULL,
    CustomerId              INT             NOT NULL,
    InvoiceNumber           VARCHAR(100)    NOT NULL,
    InvoiceDate             DATE            NOT NULL,
    DueDate                 DATE            NOT NULL,
    CurrencyCode            VARCHAR(10)     NOT NULL,
    ExchangeRate            DECIMAL(18,6)   NOT NULL        DEFAULT 1,
    SubTotal                DECIMAL(15,2)   NOT NULL        DEFAULT 0,
    DiscountAmount          DECIMAL(15,2)   NOT NULL        DEFAULT 0,
    TaxRate                 DECIMAL(5,2)    NOT NULL        DEFAULT 0,
    TaxAmount               DECIMAL(15,2)   NOT NULL        DEFAULT 0,
    Total                   DECIMAL(15,2)   NOT NULL        DEFAULT 0,
    Status                  VARCHAR(20)     NOT NULL        DEFAULT 'Draft',   -- Draft, Sent, Paid, Overdue, Cancelled
    PaymentStatus           VARCHAR(20)     NOT NULL        DEFAULT 'Unpaid',  -- Unpaid, Paid
    PaidDate                DATE            NULL,
    PaidBy                  INT             NULL,
    PaymentMethodId         INT             NULL,
    PaymentReference        VARCHAR(200)    NULL,
    DepositNumber           VARCHAR(100)    NULL,
    TransferNumber          VARCHAR(100)    NULL,
    BillingPeriodFrom       DATE            NULL,
    BillingPeriodTo         DATE            NULL,
    BillingName             NVARCHAR(200)   NULL,
    BillingEmail            VARCHAR(200)    NULL,
    Notes                   NVARCHAR(MAX)   NULL,           -- Internal notes, not printed
    ClientNotes             NVARCHAR(MAX)   NULL,           -- Visible to client on PDF
    Lang                    VARCHAR(5)      NOT NULL        DEFAULT 'es',
    SentAt                  DATETIME        NULL,
    CancelledAt             DATETIME        NULL,
    CancelledBy             INT             NULL,
    CreatedBy               INT             NOT NULL,
    CreatedAt               DATETIME        NOT NULL        DEFAULT DATEADD(hour, -6, GETUTCDATE()),
    UpdatedAt               DATETIME        NULL,
    LastModifiedBy          VARCHAR(100)    NULL,

    CONSTRAINT PK_ManualInvoiceHeaders PRIMARY KEY (ManualInvoiceHeaderId),
    CONSTRAINT UQ_ManualInvoiceHeaders_InvoiceNumber UNIQUE (InvoiceNumber),
    CONSTRAINT FK_ManualInvoiceHeaders_Customer FOREIGN KEY (CustomerId)
        REFERENCES [Corporate].[Customers] (CustomerId)
);

-- 2. Details table
CREATE TABLE [Corporate].[ManualInvoiceDetails] (
    ManualInvoiceDetailId   INT             IDENTITY(1,1)   NOT NULL,
    ManualInvoiceHeaderId   INT             NOT NULL,
    ServiceDescription      NVARCHAR(500)   NOT NULL,
    UnitLabel               VARCHAR(50)     NULL,           -- e.g. "hours", "months", "units"
    Quantity                DECIMAL(18,4)   NOT NULL        DEFAULT 1,
    UnitPrice               DECIMAL(15,2)   NOT NULL,
    DiscountPercent         DECIMAL(5,2)    NOT NULL        DEFAULT 0,
    DiscountAmount          DECIMAL(15,2)   NOT NULL        DEFAULT 0,
    LineTotal               DECIMAL(15,2)   NOT NULL,
    TaxApplicable           BIT             NOT NULL        DEFAULT 1,
    Notes                   NVARCHAR(500)   NULL,
    DisplayOrder            INT             NOT NULL        DEFAULT 0,
    CreatedAt               DATETIME        NOT NULL        DEFAULT DATEADD(hour, -6, GETUTCDATE()),

    CONSTRAINT PK_ManualInvoiceDetails PRIMARY KEY (ManualInvoiceDetailId),
    CONSTRAINT FK_ManualInvoiceDetails_Header FOREIGN KEY (ManualInvoiceHeaderId)
        REFERENCES [Corporate].[ManualInvoiceHeaders] (ManualInvoiceHeaderId)
        ON DELETE CASCADE
);

-- Indexes for common queries
CREATE INDEX IX_ManualInvoiceHeaders_CustomerId  ON [Corporate].[ManualInvoiceHeaders] (CustomerId);
CREATE INDEX IX_ManualInvoiceHeaders_Status      ON [Corporate].[ManualInvoiceHeaders] (Status);
CREATE INDEX IX_ManualInvoiceHeaders_InvoiceDate ON [Corporate].[ManualInvoiceHeaders] (InvoiceDate);
CREATE INDEX IX_ManualInvoiceDetails_HeaderId    ON [Corporate].[ManualInvoiceDetails] (ManualInvoiceHeaderId);
