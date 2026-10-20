using Finanzas.Application.Common;
using Finanzas.Domain.Recurring;
using FluentValidation;

namespace Finanzas.Application.Recurring.UpdateRecurringTemplate;

public sealed class UpdateRecurringTemplateValidator : AbstractValidator<UpdateRecurringTemplateCommand>
{
    public UpdateRecurringTemplateValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Description).NotEmpty().MaximumLength(RecurringTemplate.DescriptionMaxLength);
        RuleFor(command => command.EstimatedAmount).IsAmount();
        RuleFor(command => command.DayOfMonth).InclusiveBetween(1, 31);
    }
}
