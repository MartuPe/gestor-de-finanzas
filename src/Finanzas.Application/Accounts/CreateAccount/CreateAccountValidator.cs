using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using FluentValidation;

namespace Finanzas.Application.Accounts.CreateAccount;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Account.NameMaxLength);
        RuleFor(command => command.Kind).IsInEnum().NotEqual(AccountKind.Credit)
            .WithMessage("Las tarjetas y créditos se crean con su propio pedido.");
        RuleFor(command => command.DefaultCurrency).IsCurrencyCode();
    }
}
