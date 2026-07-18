using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class CategoryAudConfiguration : IEntityTypeConfiguration<CategoryAud>
{
    public void Configure(EntityTypeBuilder<CategoryAud> builder)
    {
        builder.ToTable("tb_category_aud");

        builder.HasKey(e => new { e.IdCategory, e.IdRevision })
            .HasName("pk_category_aud");

        builder.Property(e => e.IdCategory)
            .HasColumnName("id_category");

        builder.Property(e => e.IdRevision)
            .HasColumnName("id_revision");

        builder.Property(e => e.IdRevisionType)
            .HasColumnName("id_revision_type")
            .HasColumnType("tinyint");

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(200)
            .IsUnicode(false);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(e => e.Revision)
            .WithMany(e => e.CategoryAudits)
            .HasForeignKey(e => e.IdRevision)
            .HasConstraintName("fk_category_aud_revision")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RevisionType)
            .WithMany()
            .HasForeignKey(e => e.IdRevisionType)
            .HasConstraintName("fk_category_aud_revision_type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
