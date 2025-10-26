using Microsoft.EntityFrameworkCore;
using ModelLayer.Corporate.Entities;
using ModelLayer.Ecommerce.Entities;
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
    public DbSet<ContractService> ContractServices { get; set; }
    
    // Ecommerce DbSets
    public DbSet<CustomerSubmission> CustomerSubmissions { get; set; }
    
    // Shared DbSets
    public DbSet<ErrorLog> ErrorLogs { get; set; }

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
            entity.Property(e => e.ContractId).HasMaxLength(50).IsRequired();
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

        // Configuración de ContractService (Corporate)
        modelBuilder.Entity<ContractService>(entity =>
        {
            entity.ToTable("ContractServices", "Corporate");
            entity.HasKey(e => e.ContractServiceId);
            entity.Property(e => e.ContractServiceId).ValueGeneratedOnAdd();
            entity.Property(e => e.ContractId).HasMaxLength(50).IsRequired();
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
    }
} 