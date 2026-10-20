using Finanzas.Domain.Accounts;
using FluentValidation;

namespace Finanzas.Application.Accounts.UpdateAccount;

public sealed class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Account.NameMaxLength);
        RuleFor(command => command.UsualDueDay!.Value).InclusiveBetween(1, 31).When(command => command.UsualDueDay is not null);
        RuleFor(command => command.UsualClosingDay!.Value).InclusiveBetween(1, 31).When(command => command.UsualClosingDay is not null);
    }
}
