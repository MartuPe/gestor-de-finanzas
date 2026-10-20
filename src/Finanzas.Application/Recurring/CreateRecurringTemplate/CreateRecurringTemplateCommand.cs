using Finanzas.Domain.Recurring;

namespace Finanzas.Application.Recurring.CreateRecurringTemplate;

/// <summary>Un fijo: se genera solo como previsto cada vez que corresponde (R7).</summary>
/// <param name="ToAccountId">Solo en transferencias, como el ahorro mensual.</param>
/// <param name="CategoryId">Obligatoria en gastos e ingresos.</param>
/// <param name="Interval">Cada cuántos períodos: el aguinaldo es mensual con intervalo 6.</param>
public sealed record CreateRecurringTemplateCommand(
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
    DateOnly? EndsOn = null);
