using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using FluentValidation;

namespace Finanzas.Application.Months.GetMonthSummary;

public sealed class GetMonthSummaryHandler(
    ICurrentUser user,
    ILedgerRepository ledgers,
    IMonthReader monthReader,
    IStatementReader statementReader,
    ExchangeRateService exchangeRates,
    TimeProvider timeProvider,
    IValidator<GetMonthSummaryQuery> validator)
{
    public async Task<Result<MonthSummaryResponse>> HandleAsync(GetMonthSummaryQuery query, CancellationToken cancellationToken)
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

        var period = query.Period;
        var movements = await monthReader.GetMovementsAsync(ledger.Value.Id, period, cancellationToken);

        // Resúmenes que vencen hasta fin de mes y no están pagados, incluidos los atrasados.
        var statements = (await statementReader.GetBalancesAsync(ledger.Value.Id, null, period, openOnly: true, cancellationToken))
            .Where(statement => !statement.Charges.IsZero)
            .ToList();

        var position = new MonthPosition(
            period,
            movements.LiquidAtStart,
            movements.ConfirmedFlows,
            MoneyBag.Of(movements.Planned.Select(planned => planned.Amount)),
            statements.Select(statement => statement.Outstanding).Sum());

        var today = ledger.Value.TodayAt(timeProvider.GetUtcNow());
        var rateDate = period.LastDay < today ? period.LastDay : today;
        var currencies = position.LiquidAtStart.Currencies
            .Concat(position.ConfirmedFlows.Currencies)
            .Concat(position.PlannedPending.Currencies)
            .Concat(position.StatementsOutstanding.Currencies);
        var (toBase, rates) = await exchangeRates.GetRatesToBaseAsync(ledger.Value, currencies, rateDate, cancellationToken);

        var available = AvailableToSpendCalculator.Calculate(position, ledger.Value.BaseCurrency, toBase);

        return new MonthSummaryResponse(
            period,
            ledger.Value.BaseCurrency,
            available.ByCurrency.ToAmounts(),
            available.Unified,
            available.MissingRates,
            movements.LiquidAtStart.ToAmounts(),
            movements.Income.ToAmounts(),
            movements.Spent.ToAmounts(),
            movements.Planned
                .Select(planned => new PlannedItemResponse(
                    planned.TransactionId,
                    planned.AccountName,
                    planned.Date,
                    planned.Amount.Amount,
                    planned.Amount.Currency,
                    planned.Kind,
                    planned.Description,
                    IsOverdue: planned.Date < today))
                .ToList(),
            statements
                .Select(statement => new StatementDueResponse(
                    statement.StatementId,
                    statement.CreditAccountId,
                    statement.CreditAccountName,
                    statement.Period,
                    statement.DueDate,
                    statement.Charges.ToAmounts(),
                    statement.Outstanding.ToAmounts()))
                .ToList(),
            rates);
    }
}
