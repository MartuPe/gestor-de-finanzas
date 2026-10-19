using Finanzas.Application.Common;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;

namespace Finanzas.Infrastructure.Tests.UseCases;

/// <summary>Reemplaza a DolarApi: los tests no dependen de internet.</summary>
internal sealed class FakeRateSource : IExchangeRateSource
{
    public IReadOnlyList<ExchangeQuote> Quotes { get; set; } = [];

    public bool Fails { get; set; }

    public int Calls { get; private set; }

    public static FakeRateSource Unavailable() => new() { Fails = true };

    public Task<Result<IReadOnlyList<ExchangeQuote>>> GetQuotesAsync(CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(Fails
            ? Result<IReadOnlyList<ExchangeQuote>>.Failure(ApplicationErrors.ExchangeRatesUnavailable)
            : Result<IReadOnlyList<ExchangeQuote>>.Success(Quotes));
    }
}
