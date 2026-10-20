using Finanzas.Domain.Ledgers;
using FluentValidation;

namespace Finanzas.Application.Ledgers.CreateLedger;

public sealed class CreateLedgerValidator : AbstractValidator<CreateLedgerCommand>
{
    public CreateLedgerValidator()
    {
        RuleFor(command => command.LedgerId).NotEmpty();
        RuleFor(command => command.OwnerUserId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Ledger.NameMaxLength);
    }
}
