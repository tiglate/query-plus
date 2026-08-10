using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Configurations;

public class JobDefinitionConfiguration : IEntityTypeConfiguration<JobDefinition>
{
    public void Configure(EntityTypeBuilder<JobDefinition> builder)
    {
        builder.ToTable("tb_job_definition", tb => tb.HasCheckConstraint(
            "ck_job_definition_no_self_approval",
            "[approved_by] IS NULL OR [approved_by] <> [created_by]"));

        builder.HasKey(e => e.IdJobDefinition)
            .HasName("pk_job_definition");

        builder.Property(e => e.IdJobDefinition)
            .HasColumnName("id_job_definition")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsUnicode(false);

        builder.Property(e => e.JobType)
            .HasColumnName("job_type")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired()
            .HasConversion(new EnumToStringConverter<JobType>());

        builder.Property(e => e.ScriptPath)
            .HasColumnName("script_path")
            .HasMaxLength(1000)
            .IsUnicode(false);

        builder.Property(e => e.ScriptSha256)
            .HasColumnName("script_sha256")
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(e => e.CronExpression)
            .HasColumnName("cron_expression")
            .HasMaxLength(120)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.RunAsUser)
            .HasColumnName("run_as_user")
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.MemoryLimitMb)
            .HasColumnName("memory_limit_mb")
            .IsRequired();

        builder.Property(e => e.MaxDurationMinutes)
            .HasColumnName("max_duration_minutes")
            .IsRequired();

        builder.Property(e => e.Enabled)
            .HasColumnName("enabled")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ApprovalStatus)
            .HasColumnName("approval_status")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired()
            .HasDefaultValue(JobApprovalStatus.Draft)
            .HasConversion(new EnumToStringConverter<JobApprovalStatus>())
            // Silences EF's "no configured sentinel" warning: JobApprovalStatus has no member
            // equal to default(JobApprovalStatus) (0 - Draft is 1), so the warning is otherwise
            // spurious here, but an explicit sentinel is still needed to satisfy the validator.
            // Draft is correct as the sentinel, not just a value that avoids the warning -
            // JobDefinitionService.CreateAsync always sets new entities to Draft explicitly, which
            // is also this column's DB-level default, so treating Draft as "value not set, use the
            // database default" produces the exact same inserted row either way.
            .HasSentinel(JobApprovalStatus.Draft);

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.ApprovedBy)
            .HasColumnName("approved_by")
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(e => e.ApprovedAt)
            .HasColumnName("approved_at")
            .HasColumnType("datetime2");

        builder.Property(e => e.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000)
            .IsUnicode(false);

        builder.Property(e => e.NotifyEmails)
            .HasColumnName("notify_emails")
            .HasMaxLength(1000)
            .IsUnicode(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("uq_job_definition_name");

        // What QueryPlus.SchedulerSync polls every reconcile tick.
        builder.HasIndex(e => new { e.ApprovalStatus, e.Enabled })
            .HasDatabaseName("ix_job_definition_approved_enabled");

        // JobRun/JobRunRequest configure this relationship from their own (dependent) side.
    }
}
