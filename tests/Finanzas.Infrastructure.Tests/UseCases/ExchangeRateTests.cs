using System.Net;
using System.Text;
using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using Finanzas.Infrastructure.ExchangeRates;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Tests.UseCases;

public class ExchangeRateTests(SqlServerFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task DolarApi_se_lee_y_se_descarta_lo_incompleto()
    {
        // Mismo formato que GET https://dolarapi.com/v1/dolares; una casa desconocida y una sin compra.
        const string json = """
            [
              { "moneda": "USD", "casa": "oficial", "nombre": "Oficial", "compra": 1485, "venta": 1535, "fechaActualizacion": "2026-10-16T18:55:00.000Z" },
              { "moneda": "USD", "casa": "blue", "nombre": "Blue", "compra": 1535, "venta": 1555, "fechaActualizacion": "2026-10-18T14:57:00.000Z" },
              { "moneda": "USD", "casa": "lebac", "nombre": "Lebac", "compra": 10, "venta": 11, "fechaActualizacion": "2026-10-18T14:57:00.000Z" },
              { "moneda": "USD", "casa": "tarjeta", "nombre": "Tarjeta", "compra": null, "venta": 1995.5, "fechaActualizacion": "2026-10-16T18:55:00.000Z" }
            ]
            """;
        var client = new DolarApiClient(Http(new StubHandler(json)));

        var quotes = (await client.GetQuotesAsync(_ct)).Value;

        Assert.Equal(new[] { "oficial", "blue" }, quotes.Select(quote => quote.Source));
        var blue = quotes[1];
        Assert.Equal((1_535m, 1_555m), (blue.Buy, blue.Sell));
        Assert.Equal(new DateTimeOffset(2026, 10, 18, 14, 57, 0, TimeSpan.Zero), blue.UpdatedAt);
    }

    [Fact]
    public async Task Si_DolarApi_no_responde_es_un_error_y_no_una_excepcion()
    {
        var client = new DolarApiClient(Http(new StubHandler(null)));

        var result = await client.GetQuotesAsync(_ct);

        Assert.Equal(ApplicationErrors.ExchangeRatesUnavailable, result.Error);
    }

    [Fact]
    public async Task Actualizar_dos_veces_el_mismo_dia_pisa_la_cotizacion_sin_duplicarla()
    {
        var now = UnDiaCualquiera();
        var source = new FakeRateSource { Quotes = [Blue(1_550m, now)] };
        await using var app = App.For(fixture, Guid.NewGuid(), now, source);

        await app.WithExchangeRatesAsync(service => service.RefreshAsync(_ct));
        source.Quotes = [Blue(1_560m, now)];
        await app.WithExchangeRatesAsync(service => service.RefreshAsync(_ct));

        await using var db = fixture.CreateContext();
        var hoy = DateOnly.FromDateTime(now.DateTime);
        var filas = await db.ExchangeRates
            .Where(rate => rate.Source == ExchangeRateSources.Blue && rate.RateDate == hoy)
            .ToListAsync(_ct);
        Assert.Equal(1_560m, Assert.Single(filas).Sell);
    }

    [Fact]
    public async Task La_fecha_de_la_cotizacion_es_la_de_Argentina()
    {
        // 23:30 del día en Argentina: en UTC ya es el día siguiente.
        var now = UnDiaCualquiera().AddHours(8).AddMinutes(30);
        var source = new FakeRateSource { Quotes = [Blue(1_550m, now)] };
        await using var app = App.For(fixture, Guid.NewGuid(), now, source);

        await app.WithExchangeRatesAsync(service => service.RefreshAsync(_ct));

        var hoyEnArgentina = DateOnly.FromDateTime(now.DateTime);
        var rate = await app.WithExchangeRatesAsync(service =>
            service.GetAsync(CurrencyCodes.Usd, ExchangeRateSources.Blue, hoyEnArgentina, _ct));
        Assert.Equal(hoyEnArgentina, rate!.RateDate);
    }

    [Fact]
    public async Task Una_cotizacion_vieja_se_actualiza_al_pedirla_y_si_la_fuente_falla_se_usa_la_ultima()
    {
        var now = UnDiaCualquiera();
        var hoy = DateOnly.FromDateTime(now.DateTime);
        var source = new FakeRateSource { Quotes = [Blue(1_600m, now)] };
        await using var app = App.For(fixture, Guid.NewGuid(), now, source);
        await app.SetUsdRateAsync(ExchangeRateSources.Blue, hoy, 1_550m, now.AddHours(-2), _ct);

        var actualizada = await app.WithExchangeRatesAsync(service =>
            service.GetAsync(CurrencyCodes.Usd, ExchangeRateSources.Blue, hoy, _ct));

        Assert.Equal(1_600m, actualizada!.Sell);
        Assert.Equal(1, source.Calls);

        await app.SetUsdRateAsync(ExchangeRateSources.Blue, hoy, 1_550m, now.AddHours(-2), _ct);
        source.Fails = true;
        var ultimaConocida = await app.WithExchangeRatesAsync(service =>
            service.GetAsync(CurrencyCodes.Usd, ExchangeRateSources.Blue, hoy, _ct));

        Assert.Equal(1_550m, ultimaConocida!.Sell);
    }

    /// <summary>Un día al azar del pasado, a las 15 h de Argentina, para no pisar las cotizaciones de otros tests.</summary>
    /// <summary>
    /// Desde 2010 hasta 2023: Argentina tuvo horario de verano hasta marzo de 2009 (UTC−2) y desde entonces es siempre UTC−3,
    /// así las 23:30 de un test no caen en el día siguiente. Tampoco se pisa con las fechas fijas de los otros tests (2025-2026).
    /// </summary>
    private static DateTimeOffset UnDiaCualquiera() =>
        new DateTimeOffset(2010, 1, 1, 15, 0, 0, TimeSpan.FromHours(-3)).AddDays(Random.Shared.Next(0, 5_000));

    private static ExchangeQuote Blue(decimal sell, DateTimeOffset updatedAt) =>
        new(CurrencyCodes.Usd, ExchangeRateSources.Blue, sell - 20m, sell, updatedAt);

    private static HttpClient Http(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://dolarapi.test/") };

    /// <summary>Responde siempre el mismo JSON; sin JSON, simula que la API no responde.</summary>
    private sealed class StubHandler(string? json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            json is null
                ? throw new HttpRequestException("Sin conexión")
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
    }
}
