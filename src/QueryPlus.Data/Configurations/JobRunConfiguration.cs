using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Configurations;

public class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("tb_job_run");

        builder.HasKey(e => e.IdJobRun)
            .HasName("pk_job_run");

        builder.Property(e => e.IdJobRun)
            .HasColumnName("id_job_run")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdJobDefinition)
            .HasColumnName("id_job_definition")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired()
            .HasConversion(new EnumToStringConverter<JobRunStatus>());

        builder.Property(e => e.TriggeredBy)
            .HasColumnName("triggered_by")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired()
            .HasConversion(new EnumToStringConverter<JobTriggerSource>());

        builder.Property(e => e.RunnerPid)
            .HasColumnName("runner_pid");

        builder.Property(e => e.RunnerStartedAtUtc)
            .HasColumnName("runner_started_at_utc")
            .HasColumnType("datetime2");

        builder.Property(e => e.ChildPid)
            .HasColumnName("child_pid");

        builder.Property(e => e.ChildStartedAtUtc)
            .HasColumnName("child_started_at_utc")
            .HasColumnType("datetime2");

        builder.Property(e => e.LastHeartbeatUtc)
            .HasColumnName("last_heartbeat_utc")
            .HasColumnType("datetime2");

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("datetime2");

        builder.Property(e => e.FinishedAt)
            .HasColumnName("finished_at")
            .HasColumnType("datetime2");

        builder.Property(e => e.ExitCode)
            .HasColumnName("exit_code");

        builder.Property(e => e.StdoutPath)
            .HasColumnName("stdout_path")
            .HasMaxLength(1000)
            .IsUnicode(false);

        builder.Property(e => e.StderrPath)
            .HasColumnName("stderr_path")
            .HasMaxLength(1000)
            .IsUnicode(false);

        builder.Property(e => e.HostMachine)
            .HasColumnName("host_machine")
            .HasMaxLength(200)
            .IsUnicode(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.HasOne(e => e.JobDefinition)
            .WithMany(e => e.JobRuns)
            .HasForeignKey(e => e.IdJobDefinition)
            .HasConstraintName("fk_job_run_job_definition")
            .OnDelete(DeleteBehavior.Restrict);

        // Run-history pages, filtered/sorted by job.
        builder.HasIndex(e => new { e.IdJobDefinition, e.StartedAt })
            .HasDatabaseName("ix_job_run_job_definition_started")
            .IsDescending(false, true);

        // What the watchdog scans for stale heartbeats / missed-trigger gaps.
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_job_run_status");
    }
}
