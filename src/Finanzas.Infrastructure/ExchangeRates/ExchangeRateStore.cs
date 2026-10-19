using Finanzas.Application.ExchangeRates;
using Finanzas.Infrastructure.Persistence;
using Finanzas.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finanzas.Infrastructure.ExchangeRates;

/// <summary>
/// Las cotizaciones son datos globales: se guardan en su propio scope, así una actualización a demanda
/// nunca guarda de paso cambios a medio hacer del pedido que la disparó.
/// </summary>
internal sealed class ExchangeRateStore(IServiceScopeFactory scopes) : IExchangeRateStore
{
    public async Task<ExchangeRate?> GetLatestAsync(
        string currency,
        string source,
        DateOnly onOrBefore,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanzasDbContext>();
        var row = await db.ExchangeRates.AsNoTracking()
            .Where(rate => rate.Currency == currency && rate.Source == source && rate.RateDate <= onOrBefore)
            .OrderByDescending(rate => rate.RateDate)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new ExchangeRate(
                row.Currency,
                row.Source,
                row.RateDate,
                row.Buy,
                row.Sell,
                new DateTimeOffset(DateTime.SpecifyKind(row.FetchedAt, DateTimeKind.Utc)));
    }

    public async Task UpsertAsync(IReadOnlyCollection<ExchangeRate> rates, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanzasDbContext>();
        foreach (var rate in rates)
        {
            var row = await db.ExchangeRates.SingleOrDefaultAsync(
                existing => existing.Currency == rate.Currency && existing.Source == rate.Source && existing.RateDate == rate.RateDate,
                cancellationToken);
            if (row is null)
            {
                row = new ExchangeRateRecord { Currency = rate.Currency, Source = rate.Source, RateDate = rate.RateDate };
                db.ExchangeRates.Add(row);
            }

            row.Buy = rate.Buy;
            row.Sell = rate.Sell;
            row.FetchedAt = rate.FetchedAt.UtcDateTime;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
