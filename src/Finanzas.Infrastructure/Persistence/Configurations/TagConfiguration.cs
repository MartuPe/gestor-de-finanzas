using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Tags;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedNever();
        builder.HasOne<Ledger>().WithMany().HasForeignKey(tag => tag.LedgerId);
        builder.Property(tag => tag.Name).HasMaxLength(Tag.NameMaxLength);
        builder.Property(tag => tag.Color).IsHexColor();

        builder.HasIndex(tag => new { tag.LedgerId, tag.Name })
            .IsUnique()
            .HasFilter($"[{AuditColumns.DeletedAt}] IS NULL");
    }
}
