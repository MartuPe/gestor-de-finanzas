using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Transfers.RegisterTransfer;

public sealed class RegisterTransferValidator : AbstractValidator<RegisterTransferCommand>
{
    public RegisterTransferValidator()
    {
        RuleFor(command => command.OutId).NotEmpty();
        RuleFor(command => command.InId).NotEmpty().NotEqual(command => command.OutId);
        RuleFor(command => command.FromAccountId).NotEmpty();
        RuleFor(command => command.ToAccountId).NotEmpty();
        RuleFor(command => command.SentAmount).IsAmount();
        RuleFor(command => command.SentCurrency).IsCurrencyCode();
        RuleFor(command => command.ReceivedAmount).IsAmount();
        RuleFor(command => command.ReceivedCurrency).IsCurrencyCode();
        RuleFor(command => command.Description).IsDescription();
    }
}
