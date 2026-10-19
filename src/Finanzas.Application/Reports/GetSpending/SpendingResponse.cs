using Finanzas.Application.Abstractions;
using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Reports.GetSpending;

/// <param name="Groups">De mayor a menor gasto unificado.</param>
public sealed record SpendingResponse(
    Period Period,
    SpendingGrouping By,
    SpendingMode Mode,
    string BaseCurrency,
    IReadOnlyList<SpendingGroupResponse> Groups,
    IReadOnlyList<RateResponse> Rates);

/// <param name="Id">La categoría o la etiqueta; null para "Sin categoría" o "Sin etiqueta".</param>
/// <param name="ParentId">Categoría padre, para agrupar subcategorías en el front.</param>
/// <param name="Unified">El total en moneda base, o null si falta alguna cotización.</param>
public sealed record SpendingGroupResponse(
    Guid? Id,
    string Name,
    string? Color,
    Guid? ParentId,
    IReadOnlyDictionary<string, decimal> Amounts,
    decimal? Unified);
