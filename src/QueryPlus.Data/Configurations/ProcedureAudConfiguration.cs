using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class ProcedureAudConfiguration : IEntityTypeConfiguration<ProcedureAud>
{
    public void Configure(EntityTypeBuilder<ProcedureAud> builder)
    {
        builder.ToTable("tb_procedure_aud");

        builder.HasKey(e => new { e.IdProcedure, e.IdRevision })
            .HasName("pk_procedure_aud");

        builder.Property(e => e.IdProcedure).HasColumnName("id_procedure");
        builder.Property(e => e.IdRevision).HasColumnName("id_revision");
        builder.Property(e => e.IdRevisionType)
            .HasColumnName("id_revision_type")
            .HasColumnType("tinyint");
        builder.Property(e => e.IdCategory).HasColumnName("id_category");
        builder.Property(e => e.Caption)
            .HasColumnName("caption")
            .HasMaxLength(300)
            .IsUnicode(false);
        builder.Property(e => e.DatabaseName)
            .HasColumnName("database_name")
            .HasMaxLength(128)
            .IsUnicode(false);
        builder.Property(e => e.ProcedureName)
            .HasColumnName("procedure_name")
            .HasMaxLength(128)
            .IsUnicode(false);
        builder.Property(e => e.Enabled).HasColumnName("enabled");
        builder.Property(e => e.RoleEntitlement)
            .HasColumnName("role_entitlement")
            .HasMaxLength(100)
            .IsUnicode(false);
        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsUnicode(false);
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(e => e.Revision)
            .WithMany(e => e.ProcedureAudits)
            .HasForeignKey(e => e.IdRevision)
            .HasConstraintName("fk_procedure_aud_revision")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RevisionType)
            .WithMany()
            .HasForeignKey(e => e.IdRevisionType)
            .HasConstraintName("fk_procedure_aud_revision_type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
