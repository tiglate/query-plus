using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
    public void Configure(EntityTypeBuilder<ExecutionLog> builder)
    {
        builder.ToTable("tb_execution_log");

        builder.HasKey(e => e.IdExecutionLog)
            .HasName("pk_execution_log");

        builder.Property(e => e.IdExecutionLog)
            .HasColumnName("id_execution_log")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdProcedure)
            .HasColumnName("id_procedure")
            .IsRequired();

        builder.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsUnicode(false);

        builder.Property(e => e.ExecutionStart)
            .HasColumnName("execution_start")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.ExecutionEnd)
            .HasColumnName("execution_end")
            .HasColumnType("datetime2");

        builder.Property(e => e.Success)
            .HasColumnName("success")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ParameterValues)
            .HasColumnName("parameter_values")
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.RowCount)
            .HasColumnName("row_count");

        builder.HasOne(e => e.Procedure)
            .WithMany(e => e.ExecutionLogs)
            .HasForeignKey(e => e.IdProcedure)
            .HasConstraintName("fk_log_procedure")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.Username, e.ExecutionStart })
            .HasDatabaseName("ix_execution_log_user_date")
            .IsDescending(false, true);

        builder.HasIndex(e => new { e.IdProcedure, e.ExecutionStart })
            .HasDatabaseName("ix_execution_log_proc_date")
            .IsDescending(false, true);
    }
}
