using Finanzas.Domain.Accounts;
using Finanzas.Domain.Ledgers;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts", table =>
        {
            table.HasCheckConstraint("CK_Accounts_Kind", Sql.In<AccountKind>(nameof(Account.Kind)));
            table.HasCheckConstraint(
                "CK_Accounts_CreditNotLiquid", $"[Kind] <> {Sql.Literal(AccountKind.Credit)} OR [IsLiquid] = 0");
        });

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.HasOne<Ledger>().WithMany().HasForeignKey(account => account.LedgerId);
        builder.Property(account => account.Name).HasMaxLength(Account.NameMaxLength);
        builder.Property(account => account.DefaultCurrency).IsCurrencyCode();
        builder.HasCurrency(nameof(Account.DefaultCurrency));
        builder.Property(account => account.Color).IsHexColor();
        builder.Property(account => account.Icon).HasMaxLength(40).IsUnicode(false);
        builder.Property(account => account.SortOrder).HasConversion<short>();

        builder.HasIndex(account => new { account.LedgerId, account.Name })
            .IsUnique()
            .HasFilter($"[{AuditColumns.DeletedAt}] IS NULL");

        builder.OwnsOne(account => account.CreditLine, line =>
        {
            line.ToTable("CreditLines", table =>
            {
                table.HasCheckConstraint("CK_CreditLines_LineType", Sql.In<CreditLineType>(nameof(CreditLine.LineType)));
                table.HasCheckConstraint(
                    "CK_CreditLines_Days",
                    "[UsualDueDay] BETWEEN 1 AND 31 AND ([UsualClosingDay] IS NULL OR [UsualClosingDay] BETWEEN 1 AND 31)");
            });
            line.WithOwner().HasForeignKey(creditLine => creditLine.AccountId);
            line.HasKey(creditLine => creditLine.AccountId);
            line.Property(creditLine => creditLine.Issuer).HasMaxLength(CreditLine.IssuerMaxLength);
            line.Property(creditLine => creditLine.Network).HasMaxLength(20).IsUnicode(false);
            line.Property(creditLine => creditLine.UsualClosingDay).HasConversion<byte?>();
            line.Property(creditLine => creditLine.UsualDueDay).HasConversion<byte>();
        });
    }
}
