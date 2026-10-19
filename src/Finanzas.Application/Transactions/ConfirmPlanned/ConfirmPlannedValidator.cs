using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Transactions.ConfirmPlanned;

public sealed class ConfirmPlannedValidator : AbstractValidator<ConfirmPlannedCommand>
{
    public ConfirmPlannedValidator()
    {
        RuleFor(command => command.TransactionId).NotEmpty();
        RuleFor(command => command.Amount!.Value).IsAmount().When(command => command.Amount is not null);
    }
}
