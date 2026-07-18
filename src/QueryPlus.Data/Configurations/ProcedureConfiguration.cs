using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class ProcedureConfiguration : IEntityTypeConfiguration<Procedure>
{
    public void Configure(EntityTypeBuilder<Procedure> builder)
    {
        builder.ToTable("tb_procedure");

        builder.HasKey(e => e.IdProcedure)
            .HasName("pk_procedure");

        builder.Property(e => e.IdProcedure)
            .HasColumnName("id_procedure")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdCategory)
            .HasColumnName("id_category")
            .IsRequired();

        builder.Property(e => e.Caption)
            .HasColumnName("caption")
            .HasMaxLength(300)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.DatabaseName)
            .HasColumnName("database_name")
            .HasMaxLength(128)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ProcedureName)
            .HasColumnName("procedure_name")
            .HasMaxLength(128)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Enabled)
            .HasColumnName("enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.RoleEntitlement)
            .HasColumnName("role_entitlement")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsUnicode(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasIndex(e => e.Caption)
            .IsUnique()
            .HasDatabaseName("uq_procedure_caption");

        builder.HasIndex(e => new { e.DatabaseName, e.ProcedureName })
            .IsUnique()
            .HasDatabaseName("uq_procedure_db_proc");
    }
}
