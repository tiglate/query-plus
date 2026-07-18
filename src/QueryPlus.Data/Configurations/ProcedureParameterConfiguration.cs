using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Configurations;

public class ProcedureParameterConfiguration : IEntityTypeConfiguration<ProcedureParameter>
{
    public void Configure(EntityTypeBuilder<ProcedureParameter> builder)
    {
        builder.ToTable("tb_procedure_parameter");

        builder.HasKey(e => e.IdProcedureParameter)
            .HasName("pk_procedure_parameter");

        builder.Property(e => e.IdProcedureParameter)
            .HasColumnName("id_procedure_parameter")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdProcedure)
            .HasColumnName("id_procedure")
            .IsRequired();

        builder.Property(e => e.Caption)
            .HasColumnName("caption")
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ParameterType)
            .HasColumnName("parameter_type")
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired()
            .HasConversion(new EnumToStringConverter<ParameterType>());

        builder.Property(e => e.DefaultValue)
            .HasColumnName("default_value")
            .HasMaxLength(500)
            .IsUnicode(true);

        builder.Property(e => e.ComboValues)
            .HasColumnName("combo_values")
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IsRequired)
            .HasColumnName("is_required")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasIndex(e => new { e.IdProcedure, e.Name })
            .IsUnique()
            .HasDatabaseName("uq_parameter_procedure_name");

        builder.HasOne(e => e.Procedure)
            .WithMany(e => e.Parameters)
            .HasForeignKey(e => e.IdProcedure)
            .HasConstraintName("fk_parameter_procedure")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
