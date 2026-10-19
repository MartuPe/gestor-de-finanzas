using Finanzas.Domain.Common;

namespace Finanzas.Application.CreditLines.GetDebtSchedule;

/// <summary>
/// La deuda de tarjetas y créditos mes a mes, por línea y de todas juntas: el total de cada resumen
/// y lo que falta pagar. Incluye los cargos previstos (fijos en tarjeta).
/// </summary>
/// <param name="From">Primer mes de vencimiento a mostrar.</param>
public sealed record GetDebtScheduleQuery(Period From, int Months = 6);
