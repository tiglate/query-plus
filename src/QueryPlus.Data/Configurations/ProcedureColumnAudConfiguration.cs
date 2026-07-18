using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class ProcedureColumnAudConfiguration : IEntityTypeConfiguration<ProcedureColumnAud>
{
    public void Configure(EntityTypeBuilder<ProcedureColumnAud> builder)
    {
        builder.ToTable("tb_procedure_column_aud");

        builder.HasKey(e => new { e.IdProcedureColumn, e.IdRevision })
            .HasName("pk_column_aud");

        builder.Property(e => e.IdProcedureColumn).HasColumnName("id_procedure_column");
        builder.Property(e => e.IdRevision).HasColumnName("id_revision");
        builder.Property(e => e.IdRevisionType)
            .HasColumnName("id_revision_type")
            .HasColumnType("tinyint");
        builder.Property(e => e.IdProcedure).HasColumnName("id_procedure");
        builder.Property(e => e.TechnicalName)
            .HasColumnName("technical_name")
            .HasMaxLength(128)
            .IsUnicode(false);
        builder.Property(e => e.Caption)
            .HasColumnName("caption")
            .HasMaxLength(200)
            .IsUnicode(false);
        builder.Property(e => e.Alignment)
            .HasColumnName("alignment")
            .HasMaxLength(10)
            .IsUnicode(false);
        builder.Property(e => e.FormatMask)
            .HasColumnName("format_mask")
            .HasMaxLength(100)
            .IsUnicode(false);
        builder.Property(e => e.Visible).HasColumnName("visible");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(e => e.Revision)
            .WithMany(e => e.ProcedureColumnAudits)
            .HasForeignKey(e => e.IdRevision)
            .HasConstraintName("fk_column_aud_revision")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RevisionType)
            .WithMany()
            .HasForeignKey(e => e.IdRevisionType)
            .HasConstraintName("fk_column_aud_revision_type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
