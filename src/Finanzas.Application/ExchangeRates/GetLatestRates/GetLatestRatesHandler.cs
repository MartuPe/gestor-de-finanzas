using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;

namespace Finanzas.Application.ExchangeRates.GetLatestRates;

/// <summary>El dólar de hoy en todas las casas. Si la cotización guardada tiene más de 30 minutos, primero se actualiza.</summary>
public sealed class GetLatestRatesHandler(
    ICurrentUser user,
    ILedgerRepository ledgers,
    ExchangeRateService exchangeRates,
    TimeProvider timeProvider)
{
    public async Task<Result<LatestRatesResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var ledger = await ledgers.CurrentLedgerAsync(user, cancellationToken);
        if (ledger.IsFailure)
        {
            return ledger.Error;
        }

        var today = ledger.Value.TodayAt(timeProvider.GetUtcNow());
        var rates = new List<RateResponse>();
        foreach (var source in ExchangeRateSources.All)
        {
            if (await exchangeRates.GetAsync(CurrencyCodes.Usd, source, today, cancellationToken) is { } rate)
            {
                rates.Add(new RateResponse(rate.Currency, rate.Source, rate.RateDate, rate.Sell));
            }
        }

        return new LatestRatesResponse(ledger.Value.ReferenceRateSource, rates);
    }
}
