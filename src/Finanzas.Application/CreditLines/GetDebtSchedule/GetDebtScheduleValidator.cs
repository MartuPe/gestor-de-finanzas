using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.CreditLines.GetDebtSchedule;

public sealed class GetDebtScheduleValidator : AbstractValidator<GetDebtScheduleQuery>
{
    public const int MaxMonths = 24;

    public GetDebtScheduleValidator()
    {
        RuleFor(query => query.From).IsPeriod();
        RuleFor(query => query.Months).InclusiveBetween(1, MaxMonths);
    }
}
