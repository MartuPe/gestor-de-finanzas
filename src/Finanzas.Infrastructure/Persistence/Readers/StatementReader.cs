using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Readers;

internal sealed class StatementReader(FinanzasDbContext db) : IStatementReader
{
    public async Task<IReadOnlyList<StatementBalance>> GetBalancesAsync(
        Guid ledgerId,
        Period? from,
        Period to,
        bool openOnly,
        CancellationToken cancellationToken)
    {
        // El vencimiento cae dentro del período: filtrar por DueDate equivale a filtrar por Period.
        var statements = Headers().Where(header => header.LedgerId == ledgerId && header.DueDate <= to.LastDay);
        if (from is Period start)
        {
            statements = statements.Where(header => header.DueDate >= start.FirstDay);
        }

        if (openOnly)
        {
            statements = statements.Where(header => header.Status == StatementStatus.Open);
        }

        return await WithAmountsAsync(await statements.OrderBy(header => header.DueDate).ToListAsync(cancellationToken), cancellationToken);
    }

    public async Task<StatementBalance?> GetBalanceAsync(Guid statementId, CancellationToken cancellationToken)
    {
        var headers = await Headers().Where(header => header.StatementId == statementId).ToListAsync(cancellationToken);
        return (await WithAmountsAsync(headers, cancellationToken)).SingleOrDefault();
    }

    private IQueryable<Header> Headers() =>
        from statement in db.Statements.AsNoTracking()
        join account in db.Accounts on statement.CreditAccountId equals account.Id
        select new Header
        {
            StatementId = statement.Id,
            LedgerId = account.LedgerId,
            CreditAccountId = statement.CreditAccountId,
            CreditAccountName = account.Name,
            Period = statement.Period,
            DueDate = statement.DueDate,
            Status = statement.Status,
        };

    private async Task<IReadOnlyList<StatementBalance>> WithAmountsAsync(List<Header> headers, CancellationToken cancellationToken)
    {
        if (headers.Count == 0)
        {
            return [];
        }

        var ids = headers.Select(header => header.StatementId).ToList();

        // Las cuotas de compras borradas quedan afuera por el filtro de borrado lógico de las dos tablas.
        var charges = await (
                from installment in db.Installments.AsNoTracking()
                join transaction in db.Transactions on installment.TransactionId equals transaction.Id
                where ids.Contains(installment.StatementId)
                group installment by new { installment.StatementId, installment.Currency } into amounts
                select new { amounts.Key.StatementId, amounts.Key.Currency, Amount = amounts.Sum(installment => installment.Amount) })
            .ToListAsync(cancellationToken);

        // La pata de cada pago que entra en la cuenta de crédito es la positiva.
        var payments = await db.Transactions.AsNoTracking()
            .Where(transaction => transaction.Kind == TransactionKind.CardPayment
                && transaction.Status == TransactionStatus.Confirmed
                && transaction.Amount > 0
                && transaction.PaidStatementId != null
                && ids.Contains(transaction.PaidStatementId.Value))
            .GroupBy(transaction => new { StatementId = transaction.PaidStatementId!.Value, transaction.Currency })
            .Select(amounts => new { amounts.Key.StatementId, amounts.Key.Currency, Amount = amounts.Sum(transaction => transaction.Amount) })
            .ToListAsync(cancellationToken);

        return headers
            .Select(header => new StatementBalance(
                header.StatementId,
                header.CreditAccountId,
                header.CreditAccountName,
                header.Period,
                header.DueDate,
                header.Status,
                MoneyBag.Of(charges.Where(row => row.StatementId == header.StatementId).Select(row => new Money(row.Amount, row.Currency))),
                MoneyBag.Of(payments.Where(row => row.StatementId == header.StatementId).Select(row => new Money(row.Amount, row.Currency)))))
            .ToList();
    }

    private sealed class Header
    {
        public Guid StatementId { get; init; }

        public Guid LedgerId { get; init; }

        public Guid CreditAccountId { get; init; }

        public string CreditAccountName { get; init; } = string.Empty;

        public Period Period { get; init; }

        public DateOnly DueDate { get; init; }

        public StatementStatus Status { get; init; }
    }
}
