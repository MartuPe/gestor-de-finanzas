using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    /// <summary>Navegación sin propiedad pública: el dominio expone solo <see cref="Transaction.TagIds"/>.</summary>
    private const string TagsField = "_tags";

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", table =>
        {
            table.HasCheckConstraint("CK_Transactions_Kind", Sql.In<TransactionKind>(nameof(Transaction.Kind)));
            table.HasCheckConstraint("CK_Transactions_Status", Sql.In<TransactionStatus>(nameof(Transaction.Status)));
            table.HasCheckConstraint("CK_Transactions_AmountNotZero", "[Amount] <> 0");
            table.HasCheckConstraint("CK_Transactions_AmountSign", AmountSign());
            table.HasCheckConstraint(
                "CK_Transactions_TransferGroup",
                Sql.Iff(InKinds(TransactionKind.Transfer, TransactionKind.CardPayment), "[TransferGroupId] IS NOT NULL"));
            table.HasCheckConstraint(
                "CK_Transactions_PaidStatement",
                Sql.Iff(InKinds(TransactionKind.CardPayment), "[PaidStatementId] IS NOT NULL"));
            table.HasCheckConstraint(
                "CK_Transactions_Recurring",
                Sql.Iff("[RecurringTemplateId] IS NULL", "[RecurringPeriod] IS NULL"));
        });

        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).ValueGeneratedNever();
        builder.HasOne<Ledger>().WithMany().HasForeignKey(transaction => transaction.LedgerId);
        builder.HasOne<Account>().WithMany().HasForeignKey(transaction => transaction.AccountId);
        builder.Property(transaction => transaction.Currency).IsCurrencyCode();
        builder.HasCurrency(nameof(Transaction.Currency));
        builder.HasOne<Category>().WithMany().HasForeignKey(transaction => transaction.CategoryId);
        builder.Property(transaction => transaction.Description).HasMaxLength(Transaction.DescriptionMaxLength);
        builder.HasOne<RecurringTemplate>().WithMany().HasForeignKey(transaction => transaction.RecurringTemplateId);
        builder.HasOne<Statement>().WithMany().HasForeignKey(transaction => transaction.PaidStatementId);

        // Las cuotas y las etiquetas son parte del agregado: se cargan siempre con el movimiento.
        builder.HasMany(transaction => transaction.Installments)
            .WithOne()
            .HasForeignKey(installment => installment.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(transaction => transaction.Installments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasMany<TransactionTag>(TagsField)
            .WithOne()
            .HasForeignKey(tag => tag.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(TagsField)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(transaction => new { transaction.LedgerId, transaction.Date })
            .IncludeProperties(transaction => new
            {
                transaction.Amount,
                transaction.Currency,
                transaction.Kind,
                transaction.Status,
                transaction.CategoryId,
                transaction.AccountId,
            });
        builder.HasIndex(transaction => new { transaction.AccountId, transaction.Date });
        builder.HasIndex(transaction => transaction.TransferGroupId).HasFilter("[TransferGroupId] IS NOT NULL");
        builder.HasIndex(transaction => transaction.PaidStatementId).HasFilter("[PaidStatementId] IS NOT NULL");

        // R7: un fijo se genera una sola vez por mes y cuenta. A propósito NO se filtran los borrados:
        // si borrás un fijo generado, no vuelve a aparecer ese mes.
        builder.HasIndex(transaction => new { transaction.RecurringTemplateId, transaction.RecurringPeriod, transaction.AccountId })
            .IsUnique()
            .HasFilter("[RecurringTemplateId] IS NOT NULL");
    }

    private static string AmountSign() =>
        $"({InKinds(TransactionKind.Expense)} AND [Amount] < 0)"
        + $" OR ({InKinds(TransactionKind.Refund, TransactionKind.Income, TransactionKind.Yield)} AND [Amount] > 0)"
        + $" OR {InKinds(TransactionKind.Transfer, TransactionKind.CardPayment, TransactionKind.Opening, TransactionKind.Adjustment)}";

    private static string InKinds(params TransactionKind[] kinds) =>
        $"[Kind] IN ({string.Join(", ", kinds.Select(Sql.Literal))})";
}
