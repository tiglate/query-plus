using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Revision> Revisions => Set<Revision>();
    public DbSet<RevisionType> RevisionTypes => Set<RevisionType>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryAud> CategoryAudits => Set<CategoryAud>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<ProcedureAud> ProcedureAudits => Set<ProcedureAud>();
    public DbSet<ProcedureParameter> ProcedureParameters => Set<ProcedureParameter>();
    public DbSet<ProcedureParameterAud> ProcedureParameterAudits => Set<ProcedureParameterAud>();
    public DbSet<ProcedureColumn> ProcedureColumns => Set<ProcedureColumn>();
    public DbSet<ProcedureColumnAud> ProcedureColumnAudits => Set<ProcedureColumnAud>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // schema.sql does not FK audit rows back to principal tables (only to tb_revision / tb_revision_type).
        // EF conventions would otherwise infer FKs from id_* property names.
        RemoveConventionForeignKeys(modelBuilder, typeof(CategoryAud), typeof(Category));
        RemoveConventionForeignKeys(modelBuilder, typeof(ProcedureAud), typeof(Procedure));
        RemoveConventionForeignKeys(modelBuilder, typeof(ProcedureAud), typeof(Category));
        RemoveConventionForeignKeys(modelBuilder, typeof(ProcedureParameterAud), typeof(ProcedureParameter));
        RemoveConventionForeignKeys(modelBuilder, typeof(ProcedureParameterAud), typeof(Procedure));
        RemoveConventionForeignKeys(modelBuilder, typeof(ProcedureColumnAud), typeof(ProcedureColumn));
        RemoveConventionForeignKeys(modelBuilder, typeof(ProcedureColumnAud), typeof(Procedure));
    }

    private static void RemoveConventionForeignKeys(ModelBuilder modelBuilder, Type dependentType, Type principalType)
    {
        var entityType = modelBuilder.Model.FindEntityType(dependentType);
        if (entityType is null)
        {
            return;
        }

        var foreignKeys = entityType.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == principalType)
            .ToList();

        foreach (var foreignKey in foreignKeys)
        {
            entityType.RemoveForeignKey(foreignKey);
        }
    }
}
