using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.CreditLines.GetDebtSchedule;

public sealed class GetDebtScheduleHandler(
    ICurrentUser user,
    ILedgerRepository ledgers,
    IStatementReader statementReader,
    ExchangeRateService exchangeRates,
    TimeProvider timeProvider,
    IValidator<GetDebtScheduleQuery> validator)
{
    public async Task<Result<DebtScheduleResponse>> HandleAsync(GetDebtScheduleQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var ledger = await ledgers.CurrentLedgerAsync(user, cancellationToken);
        if (ledger.IsFailure)
        {
            return ledger.Error;
        }

        var to = query.From.AddMonths(query.Months - 1);
        var balances = (await statementReader.GetBalancesAsync(ledger.Value.Id, query.From, to, openOnly: false, cancellationToken))
            .Where(balance => !balance.Charges.IsZero)
            .ToList();

        var today = ledger.Value.TodayAt(timeProvider.GetUtcNow());
        var (toBase, rates) = await exchangeRates.GetRatesToBaseAsync(
            ledger.Value, balances.SelectMany(balance => balance.Charges.Currencies), today, cancellationToken);

        var months = Enumerable.Range(0, query.Months)
            .Select(offset => query.From.AddMonths(offset))
            .Select(period =>
            {
                var lines = balances.Where(balance => balance.Period == period).ToList();
                var total = lines.Select(line => line.Charges).Sum();
                return new DebtMonthResponse(
                    period,
                    lines
                        .Select(line => new DebtLineResponse(
                            line.CreditAccountId,
                            line.CreditAccountName,
                            line.StatementId,
                            line.DueDate,
                            line.Status,
                            line.Charges.ToAmounts(),
                            line.Outstanding.ToAmounts()))
                        .ToList(),
                    total.ToAmounts(),
                    total.ToBase(ledger.Value.BaseCurrency, toBase),
                    lines.Select(line => line.Outstanding).Sum().ToAmounts());
            })
            .ToList();

        return new DebtScheduleResponse(ledger.Value.BaseCurrency, months, rates);
    }
}
