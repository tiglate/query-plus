using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class JobDefinitionAudConfiguration : IEntityTypeConfiguration<JobDefinitionAud>
{
    public void Configure(EntityTypeBuilder<JobDefinitionAud> builder)
    {
        builder.ToTable("tb_job_definition_aud");

        builder.HasKey(e => new { e.IdJobDefinition, e.IdRevision })
            .HasName("pk_job_definition_aud");

        builder.Property(e => e.IdJobDefinition)
            .HasColumnName("id_job_definition");

        builder.Property(e => e.IdRevision)
            .HasColumnName("id_revision");

        builder.Property(e => e.IdRevisionType)
            .HasColumnName("id_revision_type")
            .HasColumnType("tinyint");

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsUnicode(false);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsUnicode(false);

        builder.Property(e => e.JobType)
            .HasColumnName("job_type")
            .HasMaxLength(20)
            .IsUnicode(false);

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
            .IsUnicode(false);

        builder.Property(e => e.RunAsUser)
            .HasColumnName("run_as_user")
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(e => e.MemoryLimitMb)
            .HasColumnName("memory_limit_mb");

        builder.Property(e => e.MaxDurationMinutes)
            .HasColumnName("max_duration_minutes");

        builder.Property(e => e.Enabled)
            .HasColumnName("enabled");

        builder.Property(e => e.ApprovalStatus)
            .HasColumnName("approval_status")
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsUnicode(false);

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
            .HasColumnType("datetime2");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(e => e.Revision)
            .WithMany(e => e.JobDefinitionAudits)
            .HasForeignKey(e => e.IdRevision)
            .HasConstraintName("fk_job_definition_aud_revision")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RevisionType)
            .WithMany()
            .HasForeignKey(e => e.IdRevisionType)
            .HasConstraintName("fk_job_definition_aud_revision_type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
