using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Statements.PayStatement;

public sealed class PayStatementValidator : AbstractValidator<PayStatementCommand>
{
    public PayStatementValidator()
    {
        RuleFor(command => command.OutId).NotEmpty();
        RuleFor(command => command.InId).NotEmpty().NotEqual(command => command.OutId);
        RuleFor(command => command.StatementId).NotEmpty();
        RuleFor(command => command.FromAccountId).NotEmpty();
        RuleFor(command => command.PaidAmount).IsAmount();
        RuleFor(command => command.PaidCurrency).IsCurrencyCode();
        RuleFor(command => command.CreditedAmount).IsAmount();
        RuleFor(command => command.CreditedCurrency).IsCurrencyCode();
    }
}
