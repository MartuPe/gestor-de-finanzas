using Finanzas.Application.ExchangeRates;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;

namespace Finanzas.Application.Months.GetMonthSummary;

/// <param name="Available">Disponible por moneda. Un valor negativo en USD quiere decir que faltan dólares.</param>
/// <param name="AvailableUnified">Disponible en moneda base (R8b), o null si falta alguna cotización.</param>
/// <param name="MissingRates">Monedas sin cotización: por eso no se pudo unificar.</param>
/// <param name="Rates">Las cotizaciones usadas, con su fecha.</param>
public sealed record MonthSummaryResponse(
    Period Period,
    string BaseCurrency,
    IReadOnlyDictionary<string, decimal> Available,
    decimal? AvailableUnified,
    IReadOnlyList<string> MissingRates,
    IReadOnlyDictionary<string, decimal> LiquidAtStart,
    IReadOnlyDictionary<string, decimal> Income,
    IReadOnlyDictionary<string, decimal> Spent,
    IReadOnlyList<PlannedItemResponse> Planned,
    IReadOnlyList<StatementDueResponse> StatementsDue,
    IReadOnlyList<RateResponse> Rates);

/// <param name="Amount">Con signo: negativo si es una salida.</param>
/// <param name="IsOverdue">Ya pasó la fecha y sigue sin confirmar.</param>
public sealed record PlannedItemResponse(
    Guid TransactionId,
    string AccountName,
    DateOnly Date,
    decimal Amount,
    string Currency,
    TransactionKind Kind,
    string? Description,
    bool IsOverdue);

/// <param name="Charges">El total del resumen, por moneda.</param>
/// <param name="Outstanding">Lo que falta pagar, por moneda: es lo que se resta del disponible.</param>
public sealed record StatementDueResponse(
    Guid StatementId,
    Guid CreditAccountId,
    string CreditAccountName,
    Period Period,
    DateOnly DueDate,
    IReadOnlyDictionary<string, decimal> Charges,
    IReadOnlyDictionary<string, decimal> Outstanding);
