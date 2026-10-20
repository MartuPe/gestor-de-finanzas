using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Ledgers.GetLedgerSettings;

/// <summary>La configuración del libro del usuario: moneda base, casa de cotización y zona horaria.</summary>
public sealed class GetLedgerSettingsHandler(ICurrentUser user, ILedgerRepository ledgers)
{
    public async Task<Result<LedgerSettingsResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var ledger = await ledgers.CurrentLedgerAsync(user, cancellationToken);
        return ledger.IsFailure ? ledger.Error : LedgerSettings.From(ledger.Value);
    }
}
