using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Readers;

internal sealed class MonthReader(FinanzasDbContext db) : IMonthReader
{
    public async Task<MonthMovements> GetMovementsAsync(Guid ledgerId, Period period, CancellationToken cancellationToken)
    {
        var first = period.FirstDay;
        var last = period.LastDay;
        var liquid =
            from transaction in db.Transactions.AsNoTracking()
            join account in db.Accounts on transaction.AccountId equals account.Id
            where transaction.LedgerId == ledgerId && account.IsLiquid
            select new { transaction, AccountName = account.Name };

        var atStart = await liquid
            .Where(item => item.transaction.Status == TransactionStatus.Confirmed && item.transaction.Date < first)
            .GroupBy(item => item.transaction.Currency)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(item => item.transaction.Amount) })
            .ToListAsync(cancellationToken);

        var month = await liquid
            .Where(item => item.transaction.Status == TransactionStatus.Confirmed
                && item.transaction.Date >= first
                && item.transaction.Date <= last)
            .GroupBy(item => new { item.transaction.Currency, item.transaction.Kind })
            .Select(group => new { group.Key.Currency, group.Key.Kind, Amount = group.Sum(item => item.transaction.Amount) })
            .ToListAsync(cancellationToken);

        // R7: los previstos vencidos siguen contando hasta que se confirmen o se borren.
        var planned = await liquid
            .Where(item => item.transaction.Status == TransactionStatus.Planned && item.transaction.Date <= last)
            .OrderBy(item => item.transaction.Date)
            .Select(item => new
            {
                item.transaction.Id,
                item.AccountName,
                item.transaction.Date,
                item.transaction.Amount,
                item.transaction.Currency,
                item.transaction.Kind,
                item.transaction.Description,
            })
            .ToListAsync(cancellationToken);

        return new MonthMovements(
            LiquidAtStart: MoneyBag.Of(atStart.Select(row => new Money(row.Amount, row.Currency))),
            ConfirmedFlows: MoneyBag.Of(month.Select(row => new Money(row.Amount, row.Currency))),
            Income: MoneyBag.Of(month
                .Where(row => row.Kind is TransactionKind.Income or TransactionKind.Yield)
                .Select(row => new Money(row.Amount, row.Currency))),
            Spent: MoneyBag.Of(month
                .Where(row => row.Kind is TransactionKind.Expense or TransactionKind.Refund)
                .Select(row => new Money(-row.Amount, row.Currency))),
            Planned: planned
                .Select(row => new PlannedMovement(
                    row.Id, row.AccountName, row.Date, new Money(row.Amount, row.Currency), row.Kind, row.Description))
                .ToList());
    }
}
