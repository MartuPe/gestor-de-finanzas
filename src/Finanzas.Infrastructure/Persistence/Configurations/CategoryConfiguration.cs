using Finanzas.Domain.Categories;
using Finanzas.Domain.Ledgers;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", table =>
            table.HasCheckConstraint("CK_Categories_Kind", Sql.In<CategoryKind>(nameof(Category.Kind))));

        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.HasOne<Ledger>().WithMany().HasForeignKey(category => category.LedgerId);
        builder.HasOne<Category>().WithMany().HasForeignKey(category => category.ParentId);
        builder.Property(category => category.Name).HasMaxLength(Category.NameMaxLength);
        builder.Property(category => category.Color).IsHexColor();
        builder.Property(category => category.Icon).HasMaxLength(40).IsUnicode(false);
        builder.Property(category => category.SortOrder).HasConversion<short>();

        // SQL Server trata los NULL como iguales en un índice único: tampoco se repiten nombres entre categorías raíz.
        builder.HasIndex(category => new { category.LedgerId, category.ParentId, category.Name })
            .IsUnique()
            .HasFilter($"[{AuditColumns.DeletedAt}] IS NULL");
    }
}
