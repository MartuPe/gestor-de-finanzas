using Finanzas.Application.Common;
using FluentValidation;

namespace Finanzas.Application.Reports.GetSpending;

public sealed class GetSpendingValidator : AbstractValidator<GetSpendingQuery>
{
    public GetSpendingValidator()
    {
        RuleFor(query => query.Period).IsPeriod();
        RuleFor(query => query.By).IsInEnum();
        RuleFor(query => query.Mode).IsInEnum();
    }
}
