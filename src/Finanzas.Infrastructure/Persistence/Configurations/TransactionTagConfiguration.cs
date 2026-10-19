using Finanzas.Domain.Tags;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class TransactionTagConfiguration : IEntityTypeConfiguration<TransactionTag>
{
    public void Configure(EntityTypeBuilder<TransactionTag> builder)
    {
        builder.ToTable("TransactionTags");
        builder.HasKey(link => new { link.TransactionId, link.TagId });
        builder.HasOne<Tag>().WithMany().HasForeignKey(link => link.TagId);
    }
}
