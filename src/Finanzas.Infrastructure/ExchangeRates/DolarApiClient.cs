using System.Net.Http.Json;
using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;

namespace Finanzas.Infrastructure.ExchangeRates;

/// <summary>
/// Cotizaciones del dólar desde DolarApi (<c>GET /v1/dolares</c>): una por casa
/// (oficial, blue, bolsa, contadoconliqui, mayorista, cripto, tarjeta).
/// </summary>
internal sealed class DolarApiClient(HttpClient http) : IExchangeRateSource
{
    public async Task<Result<IReadOnlyList<ExchangeQuote>>> GetQuotesAsync(CancellationToken cancellationToken)
    {
        List<DolarApiQuote>? quotes;
        try
        {
            quotes = await http.GetFromJsonAsync<List<DolarApiQuote>>("v1/dolares", cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return ApplicationErrors.ExchangeRatesUnavailable;
        }

        // Se descarta lo que no sirve para R9: casas desconocidas o cotizaciones incompletas.
        return (quotes ?? [])
            .Where(quote => quote.Moneda == CurrencyCodes.Usd
                && ExchangeRateSources.IsValid(quote.Casa)
                && quote.Compra > 0
                && quote.Venta > 0)
            .Select(quote => new ExchangeQuote(
                quote.Moneda, quote.Casa, quote.Compra!.Value, quote.Venta!.Value, quote.FechaActualizacion))
            .ToList();
    }

    private sealed record DolarApiQuote(
        string Moneda,
        string Casa,
        decimal? Compra,
        decimal? Venta,
        DateTimeOffset FechaActualizacion);
}
