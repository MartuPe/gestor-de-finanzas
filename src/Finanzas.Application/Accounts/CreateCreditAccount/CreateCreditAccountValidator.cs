using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using FluentValidation;

namespace Finanzas.Application.Accounts.CreateCreditAccount;

public sealed class CreateCreditAccountValidator : AbstractValidator<CreateCreditAccountCommand>
{
    public CreateCreditAccountValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Account.NameMaxLength);
        RuleFor(command => command.DefaultCurrency).IsCurrencyCode();
        RuleFor(command => command.LineType).IsInEnum();
        RuleFor(command => command.Issuer).NotEmpty().MaximumLength(CreditLine.IssuerMaxLength);
        RuleFor(command => command.UsualDueDay).InclusiveBetween(1, 31);
        RuleFor(command => command.UsualClosingDay!.Value).InclusiveBetween(1, 31).When(command => command.UsualClosingDay is not null);
    }
}
