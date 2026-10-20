namespace Finanzas.Application.ExchangeRates.GetLatestRates;

/// <param name="ReferenceSource">La casa con la que el libro convierte los totales.</param>
/// <param name="Rates">La última cotización conocida de cada casa del dólar.</param>
public sealed record LatestRatesResponse(string ReferenceSource, IReadOnlyList<RateResponse> Rates);
