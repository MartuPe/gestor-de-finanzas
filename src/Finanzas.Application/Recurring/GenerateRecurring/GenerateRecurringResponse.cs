namespace Finanzas.Application.Recurring.GenerateRecurring;

/// <param name="Skipped">Fijos que no se pudieron generar (por ejemplo, porque se borró su categoría); los demás se generan igual.</param>
public sealed record GenerateRecurringResponse(int Generated, IReadOnlyList<SkippedTemplateResponse> Skipped);

public sealed record SkippedTemplateResponse(Guid TemplateId, string Code, string Message);
