using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Readers;

/// <summary>R1: el saldo de una cuenta es la suma de sus movimientos.</summary>
internal sealed class AccountReader(FinanzasDbContext db) : IAccountReader
{
    public async Task<IReadOnlyDictionary<Guid, AccountBalance>> GetBalancesAsync(Guid ledgerId, CancellationToken cancellationToken)
    {
        var rows = await db.Transactions.AsNoTracking()
            .Where(transaction => transaction.LedgerId == ledgerId)
            .GroupBy(transaction => new { transaction.AccountId, transaction.Currency, transaction.Status })
            .Select(group => new { group.Key.AccountId, group.Key.Currency, group.Key.Status, Amount = group.Sum(item => item.Amount) })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.AccountId)
            .ToDictionary(
                account => account.Key,
                account => new AccountBalance(
                    Confirmed: MoneyBag.Of(account
                        .Where(row => row.Status == TransactionStatus.Confirmed)
                        .Select(row => new Money(row.Amount, row.Currency))),
                    Projected: MoneyBag.Of(account.Select(row => new Money(row.Amount, row.Currency)))));
    }
}
