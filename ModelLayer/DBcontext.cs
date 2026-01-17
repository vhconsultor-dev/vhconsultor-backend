using Microsoft.EntityFrameworkCore;
using ModelLayer.Corporate.Entities;
using ModelLayer.Ecommerce.Entities;
using ModelLayer.Amazon.Entities;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace ModelLayer;

public class DBcontext : DbContext
{
    public DBcontext(DbContextOptions<DBcontext> options) : base(options)
    {
    }

    // Corporate DbSets
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Country> Countries { get; set; }
    public DbSet<IndustrySector> IndustrySectors { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<PricingService> PricingServices { get; set; }
    public DbSet<ContractService> ContractServices { get; set; }
    public DbSet<ServiceBudgetRange> ServiceBudgetRanges { get; set; }
    public DbSet<ServiceAdBudgetRange> ServiceAdBudgetRanges { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<InvoiceAttachment> InvoiceAttachments { get; set; }
    
    // Ecommerce DbSets
    public DbSet<CustomerSubmission> CustomerSubmissions { get; set; }
    
    // Amazon DbSets
    public DbSet<AmazonToken> AmazonTokens { get; set; }
    
    // Shared DbSets
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserLoginHistory> UserLoginHistory { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<ModelLayer.Shared.Entities.Action> Actions { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<UserPermissionDenial> UserPermissionDenials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuración base del modelo
        modelBuilder.HasDefaultSchema("dbo");

        // Configuración de Customer (Corporate)
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers", "Corporate");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).ValueGeneratedOnAdd();
            entity.Property(e => e.CompanyName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.NIT).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CompanyType).HasMaxLength(50);
            entity.Property(e => e.PrimaryEmail).HasMaxLength(255);
            entity.Property(e => e.SecondaryEmail).HasMaxLength(255);
            entity.Property(e => e.BillingEmail).HasMaxLength(255);
            entity.Property(e => e.PrimaryPhone).HasMaxLength(50);
            entity.Property(e => e.SecondaryPhone).HasMaxLength(50);
            entity.Property(e => e.EmergencyPhone).HasMaxLength(50);
            entity.Property(e => e.Contact1Name).HasMaxLength(100);
            entity.Property(e => e.Contact1Phone).HasMaxLength(50);
            entity.Property(e => e.Contact2Name).HasMaxLength(100);
            entity.Property(e => e.Contact2Phone).HasMaxLength(50);
            entity.Property(e => e.Contact3Name).HasMaxLength(100);
            entity.Property(e => e.Contact3Phone).HasMaxLength(50);
            entity.Property(e => e.CountryId);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.SectorId);
            entity.Property(e => e.CompanySize).HasMaxLength(50);
            entity.Property(e => e.AnnualRevenue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Website).HasMaxLength(255);
            entity.Property(e => e.ClientStatus).HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.Notes);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.LastContactDate);
        });

        // Configuración de Country (Corporate)
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries", "Corporate");
            entity.HasKey(e => e.CountryId);
            entity.Property(e => e.CountryId).ValueGeneratedOnAdd();
            entity.Property(e => e.CountryCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.CountryName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
        });

        // Configuración de IndustrySector (Corporate)
        modelBuilder.Entity<IndustrySector>(entity =>
        {
            entity.ToTable("IndustrySectors", "Corporate");
            entity.HasKey(e => e.SectorId);
            entity.Property(e => e.SectorId).ValueGeneratedOnAdd();
            entity.Property(e => e.SectorName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
        });

        // Configuración de Contract (Corporate)
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("Contracts", "Corporate");
            entity.HasKey(e => e.ContractId);
            entity.Property(e => e.ContractId).ValueGeneratedOnAdd(); // INT IDENTITY(1,1)
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.ContractNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ClientLegalName).HasMaxLength(255);
            entity.Property(e => e.ClientTaxId).HasMaxLength(50);
            entity.Property(e => e.ClientNationality).HasMaxLength(100);
            entity.Property(e => e.ClientAddress);
            entity.Property(e => e.ClientPrimaryContact).HasMaxLength(255);
            entity.Property(e => e.ClientEmail).HasMaxLength(255);
            entity.Property(e => e.ClientPhone).HasMaxLength(50);
            entity.Property(e => e.ContractTypeId);
            entity.Property(e => e.ServiceDescription);
            entity.Property(e => e.FeeTypeId);
            entity.Property(e => e.FeeAmount).HasColumnType("decimal(10,4)");
            entity.Property(e => e.FeeDescription);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.ContractTerm).HasMaxLength(50);
            entity.Property(e => e.PaymentFrequency).HasMaxLength(50);
            entity.Property(e => e.PaymentDay);
            entity.Property(e => e.PaymentMethodId);
            entity.Property(e => e.SignedDate);
            entity.Property(e => e.EffectiveDate);
            entity.Property(e => e.StartDate);
            entity.Property(e => e.EndDate);
            entity.Property(e => e.AutoRenewal);
            entity.Property(e => e.RenewalTerm).HasMaxLength(50);
            entity.Property(e => e.RenewalNoticeDays);
            entity.Property(e => e.NoticePeriodDays);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.GoverningLaw).HasMaxLength(100);
            entity.Property(e => e.DisputeResolution).HasMaxLength(100);
            entity.Property(e => e.ContractualDomicile);
            entity.Property(e => e.Jurisdiction).HasMaxLength(100);
            entity.Property(e => e.Notes);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(255);
            entity.Property(e => e.DocumentUrl).HasMaxLength(500);
            entity.Property(e => e.SignedDocumentUrl).HasMaxLength(500);
        });

        // Configuración de Service (Corporate)
        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Services", "Corporate");
            entity.HasKey(e => e.ServiceId);
            entity.Property(e => e.ServiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ServiceName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ServiceDescription).HasMaxLength(1000);
            entity.Property(e => e.DefaultUnitPrice).HasColumnType("decimal(15,2)");
            entity.Property(e => e.BillingUnit).HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });
        
        // Configuración de PricingService (Corporate)
        modelBuilder.Entity<PricingService>(entity =>
        {
            entity.ToTable("PricingServices", "Corporate");
            entity.HasKey(e => e.ServiceId);
            entity.Property(e => e.ServiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ServiceKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ServiceCategory).HasMaxLength(100);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });

        // Configuración de ContractService (Corporate)
        modelBuilder.Entity<ContractService>(entity =>
        {
            entity.ToTable("ContractServices", "Corporate");
            entity.HasKey(e => e.ContractServiceId);
            entity.Property(e => e.ContractServiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ContractId).IsRequired();
            entity.Property(e => e.ServiceId);
            entity.Property(e => e.ServiceDescription);
            entity.Property(e => e.Regions);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(15,2)");
            entity.Property(e => e.Quantity).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.FinalPrice).HasColumnType("decimal(15,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.ServiceOrder);
            entity.Property(e => e.BillingFrequency).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            entity.Property(e => e.UpdatedAt);
        });

        // Configuración de CustomerSubmission
        modelBuilder.Entity<CustomerSubmission>(entity =>
        {
            entity.ToTable("CustomerSubmissions", "Ecommerce");
            entity.HasKey(e => e.SubmissionID);
            entity.Property(e => e.SubmissionID).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BrandName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.NumberOfListings).IsRequired();
            entity.Property(e => e.ProductPageLink).IsRequired();
            entity.Property(e => e.StoreLink);
            entity.Property(e => e.SelectedPlatform).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ServiceType).HasMaxLength(50);
            entity.Property(e => e.AnnualSalesRange).HasMaxLength(100);
            entity.Property(e => e.AdvertisingBudgetRange).HasMaxLength(100);
            entity.Property(e => e.PromotionalBudgetRange).HasMaxLength(100);
            entity.Property(e => e.AdditionalDetails);
            entity.Property(e => e.SubmissionDate).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            entity.Property(e => e.SubmissionType).HasMaxLength(20).IsRequired();
        });

        // Configuración de ErrorLog
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.ToTable("ErrorLogs", "Global");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ErrorNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.StackTrace);
            entity.Property(e => e.Source).HasMaxLength(255);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.RequestMethod).HasMaxLength(10);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.RequestBody);
            entity.Property(e => e.QueryString);
            entity.Property(e => e.ExceptionType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            entity.Property(e => e.Environment).HasMaxLength(50);
            entity.Property(e => e.AdditionalData);
        });

        // Configuración de User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "Global");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).ValueGeneratedOnAdd();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.IsCorporate).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsBrandPartner).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.EmailVerified).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
            entity.Property(e => e.LockedUntil);
            entity.Property(e => e.LastLogin);
            entity.Property(e => e.LastLoginIP).HasMaxLength(45);
            entity.Property(e => e.LastLoginLocation).HasMaxLength(255);
            entity.Property(e => e.LastLoginCountry).HasMaxLength(100);
            entity.Property(e => e.LastLoginCity).HasMaxLength(100);
            entity.Property(e => e.LastLoginUserAgent).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(255);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(255);
        });

        // Configuración de UserLoginHistory
        modelBuilder.Entity<UserLoginHistory>(entity =>
        {
            entity.ToTable("UserLoginHistory", "Global");
            entity.HasKey(e => e.LoginHistoryId);
            entity.Property(e => e.LoginHistoryId).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.LoginDate).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.IPAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.LoginSuccessful).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.FailureReason).HasMaxLength(255);
            entity.Property(e => e.SessionId).HasMaxLength(255);
        });

        // Configuración de Resource
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.ToTable("Resources", "Global");
            entity.HasKey(e => e.ResourceId);
            entity.Property(e => e.ResourceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ResourceName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceKey).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Module).HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });

        // Configuración de Action
        modelBuilder.Entity<ModelLayer.Shared.Entities.Action>(entity =>
        {
            entity.ToTable("Actions", "Global");
            entity.HasKey(e => e.ActionId);
            entity.Property(e => e.ActionId).ValueGeneratedOnAdd();
            entity.Property(e => e.ActionName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ActionKey).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
        });

        // Configuración de Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions", "Global");
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.PermissionId).ValueGeneratedOnAdd();
            entity.Property(e => e.ResourceId).IsRequired();
            entity.Property(e => e.ActionId).IsRequired();
            entity.Property(e => e.PermissionName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PermissionKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });

        // Configuración de Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "Global");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId).ValueGeneratedOnAdd();
            entity.Property(e => e.RoleName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RoleKey).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsSystemRole).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });

        // Configuración de RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions", "Global");
            entity.HasKey(e => e.RolePermissionId);
            entity.Property(e => e.RolePermissionId).ValueGeneratedOnAdd();
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.PermissionId).IsRequired();
            entity.Property(e => e.GrantedBy);
            entity.Property(e => e.GrantedAt).IsRequired().HasDefaultValueSql("GETDATE()");
        });

        // Configuración de UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles", "Global");
            entity.HasKey(e => e.UserRoleId);
            entity.Property(e => e.UserRoleId).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.AssignedBy);
            entity.Property(e => e.AssignedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });

        // Configuración de UserPermission
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions", "Global");
            entity.HasKey(e => e.UserPermissionId);
            entity.Property(e => e.UserPermissionId).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PermissionId).IsRequired();
            entity.Property(e => e.GrantedBy);
            entity.Property(e => e.GrantedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });

        // Configuración de UserPermissionDenial
        modelBuilder.Entity<UserPermissionDenial>(entity =>
        {
            entity.ToTable("UserPermissionDenials", "Global");
            entity.HasKey(e => e.UserPermissionDenialId);
            entity.Property(e => e.UserPermissionDenialId).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PermissionId).IsRequired();
            entity.Property(e => e.DeniedBy);
            entity.Property(e => e.DeniedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        });
        
        // Configuración de ServiceBudgetRange (Corporate)
        modelBuilder.Entity<ServiceBudgetRange>(entity =>
        {
            entity.ToTable("ServiceBudgetRanges", "Corporate");
            entity.HasKey(e => e.ServiceBudgetRangeId);
            entity.Property(e => e.ServiceBudgetRangeId).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceId).IsRequired();
            entity.Property(e => e.BusinessTypeId).IsRequired();
            entity.Property(e => e.PlatformId);
            entity.Property(e => e.MinBudgetValue).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxBudgetValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Percentage).IsRequired().HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });
        
        // Configuración de ServiceAdBudgetRange (Corporate)
        modelBuilder.Entity<ServiceAdBudgetRange>(entity =>
        {
            entity.ToTable("ServiceAdBudgetRanges", "Corporate");
            entity.HasKey(e => e.ServiceAdBudgetRangeId);
            entity.Property(e => e.ServiceAdBudgetRangeId).ValueGeneratedOnAdd();
            entity.Property(e => e.ServiceId).IsRequired();
            entity.Property(e => e.BusinessTypeId).IsRequired();
            entity.Property(e => e.PlatformId);
            entity.Property(e => e.MinAdBudgetValue).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxAdBudgetValue).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FixedQuote).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdatedAt);
        });
        
        // Configuración de Invoice (Corporate)
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoices", "Corporate");
            entity.HasKey(e => e.InvoiceId);
            entity.Property(e => e.InvoiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ContractId).IsRequired();
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100).IsRequired();
            entity.Property(e => e.InvoiceDate).IsRequired();
            entity.Property(e => e.DueDate).IsRequired();
            entity.Property(e => e.SubTotal).HasColumnType("decimal(15,2)").IsRequired();
            entity.Property(e => e.Tax).HasColumnType("decimal(15,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.Total).HasColumnType("decimal(15,2)").IsRequired();
            entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
            entity.Property(e => e.PaymentStatus).HasMaxLength(50).IsRequired().HasDefaultValue("Unpaid");
            entity.Property(e => e.PaidDate);
            entity.Property(e => e.PaidBy);
            entity.Property(e => e.PaymentMethodId);
            entity.Property(e => e.PaymentReference).HasMaxLength(255);
            entity.Property(e => e.DepositNumber).HasMaxLength(255);
            entity.Property(e => e.TransferNumber).HasMaxLength(255);
            entity.Property(e => e.Notes);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(255);
            
            // Índice único para InvoiceNumber
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            
            // Relación con Contract
            entity.HasOne(e => e.Contract)
                .WithMany()
                .HasForeignKey(e => e.ContractId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configuración de InvoiceItem (Corporate)
        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.ToTable("InvoiceItems", "Corporate");
            entity.HasKey(e => e.InvoiceItemId);
            entity.Property(e => e.InvoiceItemId).ValueGeneratedOnAdd();
            entity.Property(e => e.InvoiceId).IsRequired();
            entity.Property(e => e.ContractServiceId);
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Quantity).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(1);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(15,2)").IsRequired();
            entity.Property(e => e.Discount).HasColumnType("decimal(15,2)").IsRequired().HasDefaultValue(0);
            entity.Property(e => e.LineTotal).HasColumnType("decimal(15,2)").IsRequired();
            entity.Property(e => e.ServiceOrder);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            
            // Relación con Invoice
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.InvoiceItems)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Relación con ContractService
            entity.HasOne(e => e.ContractService)
                .WithMany()
                .HasForeignKey(e => e.ContractServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configuración de InvoiceAttachment (Corporate)
        modelBuilder.Entity<InvoiceAttachment>(entity =>
        {
            entity.ToTable("InvoiceAttachments", "Corporate");
            entity.HasKey(e => e.InvoiceAttachmentId);
            entity.Property(e => e.InvoiceAttachmentId).ValueGeneratedOnAdd();
            entity.Property(e => e.InvoiceId).IsRequired();
            entity.Property(e => e.FileUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UploadedBy).IsRequired();
            entity.Property(e => e.UploadedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            
            // Relación con Invoice
            entity.HasOne(e => e.Invoice)
                .WithMany(i => i.InvoiceAttachments)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de AmazonToken (Amazon)
        modelBuilder.Entity<AmazonToken>(entity =>
        {
            entity.ToTable("AmazonTokens", "Amazon");
            entity.HasKey(e => e.TokenId);
            entity.Property(e => e.TokenId).ValueGeneratedOnAdd();
            entity.Property(e => e.RefreshToken).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.AccessToken).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.TokenType).HasMaxLength(50).IsRequired().HasDefaultValue("bearer");
            entity.Property(e => e.ExpiresIn).IsRequired().HasDefaultValue(3600);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("DATEADD(hour, -6, GETUTCDATE())");
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.ClientId).HasMaxLength(255);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });
    }
} 