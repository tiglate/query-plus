using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;
using QueryPlus.Domain.Enums;

namespace QueryPlus.Data.Configurations;

public class RevisionTypeConfiguration : IEntityTypeConfiguration<RevisionType>
{
    public void Configure(EntityTypeBuilder<RevisionType> builder)
    {
        builder.ToTable("tb_revision_type");

        builder.HasKey(e => e.IdRevisionType)
            .HasName("pk_revision_type");

        builder.Property(e => e.IdRevisionType)
            .HasColumnName("id_revision_type")
            .HasColumnType("tinyint")
            .ValueGeneratedNever();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.HasData(
            new RevisionType { IdRevisionType = RevisionTypeCode.Insert, Description = "INSERT" },
            new RevisionType { IdRevisionType = RevisionTypeCode.Update, Description = "UPDATE" },
            new RevisionType { IdRevisionType = RevisionTypeCode.Delete, Description = "DELETE" });
    }
}
