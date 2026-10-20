using Finanzas.Application.Common;
using Finanzas.Domain.Recurring;
using FluentValidation;

namespace Finanzas.Application.Recurring.CreateRecurringTemplate;

public sealed class CreateRecurringTemplateValidator : AbstractValidator<CreateRecurringTemplateCommand>
{
    public CreateRecurringTemplateValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Description).NotEmpty().MaximumLength(RecurringTemplate.DescriptionMaxLength);
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.AccountId).NotEmpty();
        RuleFor(command => command.EstimatedAmount).IsAmount();
        RuleFor(command => command.Currency).IsCurrencyCode();
        RuleFor(command => command.Frequency).IsInEnum();
        RuleFor(command => command.Interval).InclusiveBetween(1, RecurringTemplate.MaxInterval);
        RuleFor(command => command.DayOfMonth).InclusiveBetween(1, 31);
    }
}
