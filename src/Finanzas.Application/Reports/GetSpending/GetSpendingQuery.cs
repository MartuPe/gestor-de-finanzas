using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Reports.GetSpending;

/// <summary>
/// En qué se gastó en un mes, por categoría o por etiqueta (R10). Por defecto, las compras a crédito
/// cuentan cuota por cuota en el mes en que vence su resumen.
/// </summary>
/// <remarks>Por etiqueta, una compra con dos etiquetas suma en las dos: los grupos no se suman entre sí.</remarks>
public sealed record GetSpendingQuery(
    Period Period,
    SpendingGrouping By = SpendingGrouping.Category,
    SpendingMode Mode = SpendingMode.Due);
