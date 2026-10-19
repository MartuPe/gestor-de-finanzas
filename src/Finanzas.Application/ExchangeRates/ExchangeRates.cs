using Finanzas.Domain.Common;

namespace Finanzas.Application.ExchangeRates;

/// <summary>Cuántos ARS vale 1 unidad de <see cref="Currency"/> en una casa y un día (01-modelo §4.5).</summary>
public sealed record ExchangeRate(
    string Currency,
    string Source,
    DateOnly RateDate,
    decimal Buy,
    decimal Sell,
    DateTimeOffset FetchedAt);

/// <summary>Una cotización tal como la publica la fuente externa.</summary>
public sealed record ExchangeQuote(string Currency, string Source, decimal Buy, decimal Sell, DateTimeOffset UpdatedAt);

/// <summary>La fuente externa de cotizaciones (DolarApi).</summary>
public interface IExchangeRateSource
{
    Task<Result<IReadOnlyList<ExchangeQuote>>> GetQuotesAsync(CancellationToken cancellationToken);
}

/// <summary>El historial de cotizaciones guardado en la base.</summary>
public interface IExchangeRateStore
{
    /// <summary>R9: la última cotización con fecha igual o anterior a <paramref name="onOrBefore"/>.</summary>
    Task<ExchangeRate?> GetLatestAsync(string currency, string source, DateOnly onOrBefore, CancellationToken cancellationToken);

    /// <summary>Una fila por moneda, casa y día: si ya existe, se actualiza.</summary>
    Task UpsertAsync(IReadOnlyCollection<ExchangeRate> rates, CancellationToken cancellationToken);
}
