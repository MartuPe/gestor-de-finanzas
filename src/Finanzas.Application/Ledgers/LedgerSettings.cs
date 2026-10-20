using Finanzas.Domain.Ledgers;

namespace Finanzas.Application.Ledgers;

internal static class LedgerSettings
{
    public static LedgerSettingsResponse From(Ledger ledger) =>
        new(ledger.Id, ledger.Name, ledger.BaseCurrency, ledger.ReferenceRateSource, ledger.TimeZone);
}
