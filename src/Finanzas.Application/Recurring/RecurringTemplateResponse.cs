using Finanzas.Domain.Recurring;

namespace Finanzas.Application.Recurring;

public sealed record RecurringTemplateResponse(
    Guid Id,
    string Description,
    RecurringKind Kind,
    Guid AccountId,
    Guid? ToAccountId,
    Guid? CategoryId,
    decimal EstimatedAmount,
    string Currency,
    RecurringFrequency Frequency,
    int Interval,
    int DayOfMonth,
    DateOnly StartsOn,
    DateOnly? EndsOn,
    bool IsActive)
{
    public static RecurringTemplateResponse From(RecurringTemplate template) =>
        new(
            template.Id,
            template.Description,
            template.Kind,
            template.AccountId,
            template.ToAccountId,
            template.CategoryId,
            template.EstimatedAmount,
            template.Currency,
            template.Frequency,
            template.Interval,
            template.DayOfMonth,
            template.StartsOn,
            template.EndsOn,
            template.IsActive);
}
