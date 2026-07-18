using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Configurations;

public class ProcedureColumnConfiguration : IEntityTypeConfiguration<ProcedureColumn>
{
    public void Configure(EntityTypeBuilder<ProcedureColumn> builder)
    {
        builder.ToTable("tb_procedure_column");

        builder.HasKey(e => e.IdProcedureColumn)
            .HasName("pk_procedure_column");

        builder.Property(e => e.IdProcedureColumn)
            .HasColumnName("id_procedure_column")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdProcedure)
            .HasColumnName("id_procedure")
            .IsRequired();

        builder.Property(e => e.TechnicalName)
            .HasColumnName("technical_name")
            .HasMaxLength(128)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Caption)
            .HasColumnName("caption")
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Alignment)
            .HasColumnName("alignment")
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired()
            .HasDefaultValue(ColumnAlignment.Left)
            .HasConversion(new EnumToStringConverter<ColumnAlignment>());

        builder.Property(e => e.FormatMask)
            .HasColumnName("format_mask")
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(e => e.Visible)
            .HasColumnName("visible")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasIndex(e => new { e.IdProcedure, e.TechnicalName })
            .IsUnique()
            .HasDatabaseName("uq_column_procedure_tech");

        builder.HasOne(e => e.Procedure)
            .WithMany(e => e.Columns)
            .HasForeignKey(e => e.IdProcedure)
            .HasConstraintName("fk_column_procedure")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
