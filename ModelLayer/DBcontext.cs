using Microsoft.EntityFrameworkCore;

namespace ModelLayer;

public class DBcontext : DbContext
{
    public DBcontext(DbContextOptions<DBcontext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuración base del modelo
        modelBuilder.HasDefaultSchema("dbo");
    }
} 