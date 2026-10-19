using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;

namespace Finanzas.Application.ExchangeRates;

public sealed record RateResponse(string Currency, string Source, DateOnly RateDate, decimal Sell);

/// <summary>
/// Cotizaciones con actualización a demanda (02-backend §6): si se pide la de hoy y la guardada
/// tiene más de <see cref="MaxAge"/>, primero se consulta la fuente. Si la fuente falla,
/// se usa la última conocida.
/// </summary>
public sealed class ExchangeRateService(IExchangeRateStore store, IExchangeRateSource source, TimeProvider timeProvider)
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    private static readonly TimeZoneInfo Argentina = TimeZoneInfo.FindSystemTimeZoneById(Ledger.DefaultTimeZone);

    /// <summary>R9: la última cotización de esa casa con fecha igual o anterior a <paramref name="date"/>.</summary>
    public async Task<ExchangeRate?> GetAsync(string currency, string rateSource, DateOnly date, CancellationToken cancellationToken)
    {
        if (date >= LocalDate(timeProvider.GetUtcNow()))
        {
            var latest = await store.GetLatestAsync(currency, rateSource, date, cancellationToken);
            if (latest is null || timeProvider.GetUtcNow() - latest.FetchedAt > MaxAge)
            {
                // Si falla, seguimos con la última conocida: la fecha va en la respuesta.
                await RefreshAsync(cancellationToken);
            }
        }

        return await store.GetLatestAsync(currency, rateSource, date, cancellationToken);
    }

    /// <summary>
    /// Cuántas unidades de la moneda base vale cada una de las otras monedas, con la casa del libro
    /// y la cotización vigente en <paramref name="date"/>. Las que no tienen cotización no aparecen.
    /// </summary>
    public async Task<(IReadOnlyDictionary<string, decimal> ToBase, IReadOnlyList<RateResponse> Used)> GetRatesToBaseAsync(
        Ledger ledger,
        IEnumerable<string> currencies,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var toBase = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var used = new List<RateResponse>();
        foreach (var currency in currencies.Where(currency => currency != ledger.BaseCurrency).Distinct())
        {
            var rate = await GetAsync(currency, ledger.ReferenceRateSource, date, cancellationToken);
            if (rate is not null)
            {
                toBase[currency] = rate.Sell;
                used.Add(new RateResponse(rate.Currency, rate.Source, rate.RateDate, rate.Sell));
            }
        }

        return (toBase, used);
    }

    /// <summary>Trae las cotizaciones de la fuente y las guarda con la fecha local de Argentina.</summary>
    public async Task<Result<int>> RefreshAsync(CancellationToken cancellationToken)
    {
        var quotes = await source.GetQuotesAsync(cancellationToken);
        if (quotes.IsFailure)
        {
            return quotes.Error;
        }

        var now = timeProvider.GetUtcNow();
        var rates = quotes.Value
            .Select(quote => new ExchangeRate(
                quote.Currency, quote.Source, LocalDate(quote.UpdatedAt), quote.Buy, quote.Sell, now))
            .ToList();

        if (rates.Count == 0)
        {
            return ApplicationErrors.ExchangeRatesUnavailable;
        }

        await store.UpsertAsync(rates, cancellationToken);
        return rates.Count;
    }

    private static DateOnly LocalDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Argentina).DateTime);
}
