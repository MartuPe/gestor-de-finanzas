using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Ledgers.UpdateLedgerSettings;

/// <remarks>No tiene validador propio: el dominio valida la casa y la zona horaria.</remarks>
public sealed class UpdateLedgerSettingsHandler(ICurrentUser user, ILedgerRepository ledgers, IUnitOfWork unitOfWork)
{
    public async Task<Result<LedgerSettingsResponse>> HandleAsync(
        UpdateLedgerSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var ledger = await ledgers.CurrentLedgerAsync(user, cancellationToken);
        if (ledger.IsFailure)
        {
            return ledger.Error;
        }

        var changed = ledger.Value.ChangeSettings(command.ReferenceRateSource, command.TimeZone);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error : LedgerSettings.From(ledger.Value);
    }
}
