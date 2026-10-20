namespace Finanzas.Application.Ledgers;

public sealed record LedgerSettingsResponse(
    Guid Id,
    string Name,
    string BaseCurrency,
    string ReferenceRateSource,
    string TimeZone);
