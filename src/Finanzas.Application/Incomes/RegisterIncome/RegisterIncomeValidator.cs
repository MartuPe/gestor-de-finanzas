using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Incomes.RegisterIncome;

public sealed class RegisterIncomeValidator : AbstractValidator<RegisterIncomeCommand>
{
    public RegisterIncomeValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Amount).IsAmount();
        RuleFor(command => command.Currency).IsCurrencyCode();
        RuleFor(command => command.Description).IsDescription();
        RuleForEach(command => command.TagIds).NotEmpty();
    }
}
