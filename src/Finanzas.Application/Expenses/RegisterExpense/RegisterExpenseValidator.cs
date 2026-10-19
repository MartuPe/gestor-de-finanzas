using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Expenses.RegisterExpense;

public sealed class RegisterExpenseValidator : AbstractValidator<RegisterExpenseCommand>
{
    public RegisterExpenseValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Amount).IsAmount();
        RuleFor(command => command.Currency).IsCurrencyCode();
        RuleFor(command => command.Description).IsDescription();
        RuleFor(command => command.Installments).IsInstallmentCount();
        RuleForEach(command => command.TagIds).NotEmpty();
    }
}
