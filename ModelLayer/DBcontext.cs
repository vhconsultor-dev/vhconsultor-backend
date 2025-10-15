using Microsoft.EntityFrameworkCore;
using ModelLayer.Ecommerce.Entities;
using ModelLayer.Shared;

namespace ModelLayer;

public class DBcontext : DbContext
{
    public DBcontext(DbContextOptions<DBcontext> options) : base(options)
    {
    }

    // Ecommerce DbSets
    public DbSet<CustomerSubmission> CustomerSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuración base del modelo
        modelBuilder.HasDefaultSchema("dbo");

        // Configuración de CustomerSubmission
        modelBuilder.Entity<CustomerSubmission>(entity =>
        {
            entity.ToTable("CustomerSubmissions", "Ecommerce");
            entity.HasKey(e => e.SubmissionID);
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
    }
} 