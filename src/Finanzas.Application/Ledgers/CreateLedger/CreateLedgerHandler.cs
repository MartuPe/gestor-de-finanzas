using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using FluentValidation;

namespace Finanzas.Application.Ledgers.CreateLedger;

public sealed class CreateLedgerHandler(
    ILedgerRepository ledgers,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<CreateLedgerCommand> validator)
{
    public async Task<Result<LedgerSettingsResponse>> HandleAsync(CreateLedgerCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var ledger = Ledger.Create(command.LedgerId, command.Name, command.OwnerUserId);
        if (ledger.IsFailure)
        {
            return ledger.Error;
        }

        ledgers.Add(ledger.Value);
        categories.AddRange(DefaultCategories.For(ledger.Value.Id));
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error : LedgerSettings.From(ledger.Value);
    }
}
