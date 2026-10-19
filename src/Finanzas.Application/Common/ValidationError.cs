using Finanzas.Domain.Common;

namespace Finanzas.Application.Common;

/// <summary>Datos mal formados (HTTP 400), con el detalle por campo.</summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Fields)
    : Error("request.invalid", "Hay datos inválidos.", ErrorType.Validation);
