using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class JobRunRequestConfiguration : IEntityTypeConfiguration<JobRunRequest>
{
    public void Configure(EntityTypeBuilder<JobRunRequest> builder)
    {
        builder.ToTable("tb_job_run_request");

        builder.HasKey(e => e.IdJobRunRequest)
            .HasName("pk_job_run_request");

        builder.Property(e => e.IdJobRunRequest)
            .HasColumnName("id_job_run_request")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdJobDefinition)
            .HasColumnName("id_job_definition")
            .IsRequired();

        builder.Property(e => e.RequestedBy)
            .HasColumnName("requested_by")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.RequestedAt)
            .HasColumnName("requested_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.ConsumedAt)
            .HasColumnName("consumed_at")
            .HasColumnType("datetime2");

        builder.Property(e => e.IdJobRun)
            .HasColumnName("id_job_run");

        builder.HasOne(e => e.JobDefinition)
            .WithMany(e => e.JobRunRequests)
            .HasForeignKey(e => e.IdJobDefinition)
            .HasConstraintName("fk_job_run_request_job_definition")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.JobRun)
            .WithOne()
            .HasForeignKey<JobRunRequest>(e => e.IdJobRun)
            .HasConstraintName("fk_job_run_request_job_run")
            .OnDelete(DeleteBehavior.SetNull);

        // What QueryPlus.SchedulerSync drains each reconcile tick - keeps the "pending" query
        // cheap regardless of how much request history has accumulated.
        builder.HasIndex(e => e.ConsumedAt)
            .HasDatabaseName("ix_job_run_request_pending")
            .HasFilter("[consumed_at] IS NULL");
    }
}
