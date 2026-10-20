using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Accounts.SetOpeningBalance;

public sealed class SetOpeningBalanceValidator : AbstractValidator<SetOpeningBalanceCommand>
{
    public SetOpeningBalanceValidator()
    {
        RuleFor(command => command.TransactionId).NotEmpty();
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.Amount)
            .NotEqual(0m)
            .WithMessage("El saldo inicial no puede ser cero.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithMessage("El monto admite hasta 2 decimales.");
        RuleFor(command => command.Currency).IsCurrencyCode();
    }
}
