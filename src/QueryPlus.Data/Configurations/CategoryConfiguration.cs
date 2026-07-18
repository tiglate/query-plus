using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryPlus.Domain.Entities;

namespace QueryPlus.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("tb_category");

        builder.HasKey(e => e.IdCategory)
            .HasName("pk_category");

        builder.Property(e => e.IdCategory)
            .HasColumnName("id_category")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2")
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasIndex(e => e.Description)
            .IsUnique()
            .HasDatabaseName("uq_category_description");

        builder.HasMany(e => e.Procedures)
            .WithOne(e => e.Category)
            .HasForeignKey(e => e.IdCategory)
            .HasConstraintName("fk_procedure_category")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
