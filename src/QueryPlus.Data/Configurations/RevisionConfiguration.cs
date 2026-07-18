using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class RevisionConfiguration : IEntityTypeConfiguration<Revision>
{
    public void Configure(EntityTypeBuilder<Revision> builder)
    {
        builder.ToTable("tb_revision");

        builder.HasKey(e => e.IdRevision)
            .HasName("pk_revision");

        builder.Property(e => e.IdRevision)
            .HasColumnName("id_revision")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.RevisionTimestamp)
            .HasColumnName("revision_timestamp")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsUnicode(false);
    }
}
