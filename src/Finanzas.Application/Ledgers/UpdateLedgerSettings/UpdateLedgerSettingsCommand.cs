namespace Finanzas.Application.Ledgers.UpdateLedgerSettings;

/// <param name="ReferenceRateSource">Casa con la que se convierten los totales: blue, oficial, bolsa, …</param>
/// <param name="TimeZone">Zona horaria IANA, por ejemplo America/Argentina/Buenos_Aires.</param>
public sealed record UpdateLedgerSettingsCommand(string ReferenceRateSource, string TimeZone);
