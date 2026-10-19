using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Months.GetMonthSummary;

public sealed class GetMonthSummaryValidator : AbstractValidator<GetMonthSummaryQuery>
{
    public GetMonthSummaryValidator() => RuleFor(query => query.Period).IsPeriod();
}
