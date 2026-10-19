using Finanzas.Domain.Common;

namespace Finanzas.Application.Months.GetMonthSummary;

/// <summary>
/// La pantalla principal: el "Disponible para gastar" de tu Excel (R8, R8b) y lo que lo explica.
/// Antes conviene generar los fijos del mes (<c>GenerateRecurringCommand</c>), así entran como previstos.
/// </summary>
public sealed record GetMonthSummaryQuery(Period Period);
