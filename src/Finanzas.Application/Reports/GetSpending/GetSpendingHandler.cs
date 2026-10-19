using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Reports.GetSpending;

public sealed class GetSpendingHandler(
    ICurrentUser user,
    ILedgerRepository ledgers,
    ICategoryRepository categories,
    ITagRepository tags,
    ISpendingReader spendingReader,
    ExchangeRateService exchangeRates,
    TimeProvider timeProvider,
    IValidator<GetSpendingQuery> validator)
{
    public async Task<Result<SpendingResponse>> HandleAsync(GetSpendingQuery query, CancellationToken cancellationToken)
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

        var rows = await spendingReader.GetAsync(ledger.Value.Id, query.Period, query.By, query.Mode, cancellationToken);
        var names = await GroupNamesAsync(query.By, cancellationToken);

        var today = ledger.Value.TodayAt(timeProvider.GetUtcNow());
        var rateDate = query.Period.LastDay < today ? query.Period.LastDay : today;
        var (toBase, rates) = await exchangeRates.GetRatesToBaseAsync(
            ledger.Value, rows.Select(row => row.Amount.Currency), rateDate, cancellationToken);

        var groups = rows
            .GroupBy(row => row.GroupId)
            .Select(group =>
            {
                var amounts = MoneyBag.Of(group.Select(row => row.Amount));
                var (name, color, parentId) = group.Key is Guid id && names.TryGetValue(id, out var info)
                    ? info
                    : (query.By == SpendingGrouping.Category ? "Sin categoría" : "Sin etiqueta", null, null);
                return new SpendingGroupResponse(
                    group.Key, name, color, parentId, amounts.ToAmounts(), amounts.ToBase(ledger.Value.BaseCurrency, toBase));
            })
            .OrderByDescending(group => group.Unified ?? decimal.MinValue)
            .ToList();

        return new SpendingResponse(query.Period, query.By, query.Mode, ledger.Value.BaseCurrency, groups, rates);
    }

    private async Task<Dictionary<Guid, (string Name, string? Color, Guid? ParentId)>> GroupNamesAsync(
        SpendingGrouping by,
        CancellationToken cancellationToken) =>
        by == SpendingGrouping.Category
            ? (await categories.ListAsync(user.LedgerId, cancellationToken))
                .ToDictionary(category => category.Id, category => (category.Name, category.Color, category.ParentId))
            : (await tags.ListAsync(user.LedgerId, cancellationToken))
                .ToDictionary(tag => tag.Id, tag => (tag.Name, tag.Color, (Guid?)null));
}
