using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class ProcedureParameterAudConfiguration : IEntityTypeConfiguration<ProcedureParameterAud>
{
    public void Configure(EntityTypeBuilder<ProcedureParameterAud> builder)
    {
        builder.ToTable("tb_procedure_parameter_aud");

        builder.HasKey(e => new { e.IdProcedureParameter, e.IdRevision })
            .HasName("pk_parameter_aud");

        builder.Property(e => e.IdProcedureParameter).HasColumnName("id_procedure_parameter");
        builder.Property(e => e.IdRevision).HasColumnName("id_revision");
        builder.Property(e => e.IdRevisionType)
            .HasColumnName("id_revision_type")
            .HasColumnType("tinyint");
        builder.Property(e => e.IdProcedure).HasColumnName("id_procedure");
        builder.Property(e => e.Caption)
            .HasColumnName("caption")
            .HasMaxLength(200)
            .IsUnicode(false);
        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsUnicode(false);
        builder.Property(e => e.ParameterType)
            .HasColumnName("parameter_type")
            .HasMaxLength(50)
            .IsUnicode(false);
        builder.Property(e => e.DefaultValue)
            .HasColumnName("default_value")
            .HasMaxLength(500)
            .IsUnicode(true);
        builder.Property(e => e.ComboValues)
            .HasColumnName("combo_values")
            .HasColumnType("nvarchar(max)");
        builder.Property(e => e.IsRequired)
            .HasColumnName("is_required");
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");
        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(e => e.Revision)
            .WithMany(e => e.ProcedureParameterAudits)
            .HasForeignKey(e => e.IdRevision)
            .HasConstraintName("fk_parameter_aud_revision")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RevisionType)
            .WithMany()
            .HasForeignKey(e => e.IdRevisionType)
            .HasConstraintName("fk_parameter_aud_revision_type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
