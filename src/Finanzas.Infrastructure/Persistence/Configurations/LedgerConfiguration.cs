using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class LedgerConfiguration : IEntityTypeConfiguration<Ledger>
{
    public void Configure(EntityTypeBuilder<Ledger> builder)
    {
        builder.ToTable("Ledgers", table =>
            table.HasCheckConstraint(
                "CK_Ledgers_ReferenceRateSource", Sql.In(nameof(Ledger.ReferenceRateSource), ExchangeRateSources.All)));

        builder.HasKey(ledger => ledger.Id);
        builder.Property(ledger => ledger.Id).ValueGeneratedNever();
        builder.Property(ledger => ledger.Name).HasMaxLength(Ledger.NameMaxLength);
        builder.Property(ledger => ledger.BaseCurrency).IsCurrencyCode();
        builder.HasCurrency(nameof(Ledger.BaseCurrency));
        builder.Property(ledger => ledger.ReferenceRateSource).HasMaxLength(20).IsUnicode(false);
        builder.Property(ledger => ledger.TimeZone).HasMaxLength(64).IsUnicode(false);

        // La FK de UserId a Users se agrega con ASP.NET Core Identity (02-backend §5).
        builder.OwnsMany(ledger => ledger.Members, members =>
        {
            members.ToTable("LedgerMembers", table =>
                table.HasCheckConstraint("CK_LedgerMembers_Role", Sql.In<LedgerRole>(nameof(LedgerMember.Role))));
            members.WithOwner().HasForeignKey(member => member.LedgerId);
            members.HasKey(member => new { member.LedgerId, member.UserId });
            members.HasIndex(member => member.UserId);
        });
        builder.Navigation(ledger => ledger.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
