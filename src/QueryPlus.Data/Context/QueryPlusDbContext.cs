using Microsoft.EntityFrameworkCore;
using QueryPlus.Domain.Common;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Context;

public class QueryPlusDbContext : DbContext
{
    public QueryPlusDbContext(DbContextOptions<QueryPlusDbContext> options)
        : base(options)
    {
    }

    public DbSet<QueryDefinition> QueryDefinitions => Set<QueryDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<QueryDefinition>(entity =>
        {
            entity.ToTable("QueryDefinitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.StoredProcedureName).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
