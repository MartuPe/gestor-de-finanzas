using System.Net.Http.Json;
using System.Text.Json;
using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Finanzas.Api.IntegrationTests;

/// <summary>
/// La Api completa sobre una base nueva, con un cliente HTTPS que guarda la cookie de sesión como un navegador.
/// Reloj fijo en el 18/10/2026 y sin acceso a DolarApi.
/// </summary>
internal sealed class TestApi : IAsyncDisposable
{
    public static readonly DateTimeOffset Today = new(2026, 10, 18, 15, 0, 0, TimeSpan.FromHours(-3));

    public const string Email = "martu@test.local";
    public const string Password = "una-clave-larga-1";

    private readonly WebApplicationFactory<Program> _factory;

    private TestApi(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
    }

    public HttpClient Client { get; }

    public static async Task<TestApi> StartAsync(SqlServerFixture sql, CancellationToken cancellationToken)
    {
        var connectionString = await sql.NewDatabaseAsync(cancellationToken);
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // "Testing": sin los user-secrets de desarrollo.
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Finanzas", connectionString);
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(Today)));
                services.Replace(ServiceDescriptor.Singleton<IExchangeRateSource>(new UnavailableRates()));
            });
        });

        return new TestApi(factory);
    }

    /// <summary>Configuración inicial: crea el usuario y el libro, y deja la sesión iniciada.</summary>
    public async Task SetupAsync(CancellationToken cancellationToken)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/setup", new { email = Email, password = Password, displayName = "Martu" }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<JsonElement> GetJsonAsync(string url, CancellationToken cancellationToken)
    {
        var response = await Client.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    public async Task<JsonElement> PostJsonAsync(string url, object body, CancellationToken cancellationToken)
    {
        var response = await Client.PostAsJsonAsync(url, body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    /// <summary>Como <c>EnsureSuccessStatusCode</c>, pero con el cuerpo de la respuesta: ahí viene el <c>code</c> del error.</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"{(int)response.StatusCode} en {response.RequestMessage?.RequestUri}: {body}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    private sealed class UnavailableRates : IExchangeRateSource
    {
        public Task<Result<IReadOnlyList<ExchangeQuote>>> GetQuotesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Result<IReadOnlyList<ExchangeQuote>>.Failure(ApplicationErrors.ExchangeRatesUnavailable));
    }
}
