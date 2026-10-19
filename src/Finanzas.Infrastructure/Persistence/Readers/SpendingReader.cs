using Finanzas.Application.Abstractions;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Readers;

/// <summary>R10: qué cuenta como gasto en un mes, según el modo.</summary>
internal sealed class SpendingReader(FinanzasDbContext db) : ISpendingReader
{
    public async Task<IReadOnlyList<SpendingRow>> GetAsync(
        Guid ledgerId,
        Period period,
        SpendingGrouping by,
        SpendingMode mode,
        CancellationToken cancellationToken)
    {
        var items = mode == SpendingMode.Purchase ? ByPurchaseDate(ledgerId, period) : ByDueMonth(ledgerId, period);

        if (by == SpendingGrouping.Category)
        {
            var byCategory = await items
                .GroupBy(item => new { item.CategoryId, item.Currency })
                .Select(group => new { GroupId = group.Key.CategoryId, group.Key.Currency, Amount = group.Sum(item => item.Amount) })
                .ToListAsync(cancellationToken);
            return byCategory.Select(row => new SpendingRow(row.GroupId, new Money(row.Amount, row.Currency))).ToList();
        }

        // Una compra con varias etiquetas suma en cada una; la que no tiene ninguna va a "sin etiqueta".
        var byTag = await (
                from item in items
                from link in db.Set<TransactionTag>().Where(link => link.TransactionId == item.TransactionId).DefaultIfEmpty()
                group item by new { TagId = (Guid?)link!.TagId, item.Currency } into amounts
                select new { GroupId = amounts.Key.TagId, amounts.Key.Currency, Amount = amounts.Sum(item => item.Amount) })
            .ToListAsync(cancellationToken);
        return byTag.Select(row => new SpendingRow(row.GroupId, new Money(row.Amount, row.Currency))).ToList();
    }

    /// <summary>Gastos y devoluciones confirmados del libro, de cualquier cuenta.</summary>
    private IQueryable<Transaction> Expenses(Guid ledgerId) =>
        db.Transactions.AsNoTracking()
            .Where(transaction => transaction.LedgerId == ledgerId
                && transaction.Status == TransactionStatus.Confirmed
                && (transaction.Kind == TransactionKind.Expense || transaction.Kind == TransactionKind.Refund));

    /// <summary>Cada compra entera, en el mes en que se hizo.</summary>
    private IQueryable<Item> ByPurchaseDate(Guid ledgerId, Period period)
    {
        var first = period.FirstDay;
        var last = period.LastDay;
        return Expenses(ledgerId)
            .Where(transaction => transaction.Date >= first && transaction.Date <= last)
            .Select(transaction => new Item
            {
                TransactionId = transaction.Id,
                CategoryId = transaction.CategoryId,
                Currency = transaction.Currency,
                Amount = -transaction.Amount,
            });
    }

    /// <summary>
    /// Lo pagado con dinero, en el mes del gasto; lo comprado a crédito, cuota por cuota en el mes en que vence su resumen.
    /// </summary>
    private IQueryable<Item> ByDueMonth(Guid ledgerId, Period period)
    {
        var first = period.FirstDay;
        var last = period.LastDay;

        var cash =
            from transaction in Expenses(ledgerId)
            join account in db.Accounts on transaction.AccountId equals account.Id
            where account.Kind != AccountKind.Credit && transaction.Date >= first && transaction.Date <= last
            select new Item
            {
                TransactionId = transaction.Id,
                CategoryId = transaction.CategoryId,
                Currency = transaction.Currency,
                Amount = -transaction.Amount,
            };

        var credit =
            from installment in db.Installments
            join statement in db.Statements on installment.StatementId equals statement.Id
            join transaction in Expenses(ledgerId) on installment.TransactionId equals transaction.Id
            where statement.DueDate >= first && statement.DueDate <= last
            select new Item
            {
                TransactionId = transaction.Id,
                CategoryId = transaction.CategoryId,
                Currency = installment.Currency,
                Amount = installment.Amount,
            };

        return cash.Concat(credit);
    }

    private sealed class Item
    {
        public Guid TransactionId { get; init; }

        public Guid? CategoryId { get; init; }

        public string Currency { get; init; } = string.Empty;

        public decimal Amount { get; init; }
    }
}
