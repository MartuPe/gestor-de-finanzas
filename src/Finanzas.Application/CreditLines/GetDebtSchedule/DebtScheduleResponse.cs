using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;

namespace Finanzas.Application.CreditLines.GetDebtSchedule;

/// <param name="Rates">Cotizaciones de hoy, con las que se unifican los totales.</param>
public sealed record DebtScheduleResponse(
    string BaseCurrency,
    IReadOnlyList<DebtMonthResponse> Months,
    IReadOnlyList<RateResponse> Rates);

/// <param name="Lines">Un resumen por tarjeta o crédito que vence ese mes.</param>
/// <param name="Total">La suma de los resúmenes del mes, por moneda.</param>
/// <param name="TotalUnified">Esa suma en moneda base, o null si falta alguna cotización.</param>
/// <param name="Outstanding">Lo que falta pagar de esos resúmenes, por moneda.</param>
public sealed record DebtMonthResponse(
    Period Period,
    IReadOnlyList<DebtLineResponse> Lines,
    IReadOnlyDictionary<string, decimal> Total,
    decimal? TotalUnified,
    IReadOnlyDictionary<string, decimal> Outstanding);

public sealed record DebtLineResponse(
    Guid CreditAccountId,
    string Name,
    Guid StatementId,
    DateOnly DueDate,
    StatementStatus Status,
    IReadOnlyDictionary<string, decimal> Charges,
    IReadOnlyDictionary<string, decimal> Outstanding);
