using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Recurring.GenerateRecurring;

public sealed class GenerateRecurringValidator : AbstractValidator<GenerateRecurringCommand>
{
    public GenerateRecurringValidator() => RuleFor(command => command.Period).IsPeriod();
}
