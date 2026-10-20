namespace Finanzas.Application.Recurring.UpdateRecurringTemplate;

/// <summary>
/// Corrige un fijo (el sueldo aumentó, cambió el día) o lo pausa. Vale desde el próximo mes que se genere:
/// los previstos ya generados se corrigen al confirmarlos.
/// </summary>
public sealed record UpdateRecurringTemplateCommand(
    Guid Id,
    string Description,
    decimal EstimatedAmount,
    int DayOfMonth,
    DateOnly? EndsOn,
    bool IsActive);
