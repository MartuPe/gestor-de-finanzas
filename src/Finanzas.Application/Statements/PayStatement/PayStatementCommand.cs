namespace Finanzas.Application.Statements.PayStatement;

/// <summary>
/// Paga un resumen, total o parcialmente. La plata sale de <see cref="FromAccountId"/> en
/// <see cref="PaidCurrency"/> y se acredita en la tarjeta en <see cref="CreditedCurrency"/>:
/// por ejemplo, la parte en dólares pagada con dólares propios, o con pesos.
/// </summary>
public sealed record PayStatementCommand(
    Guid OutId,
    Guid InId,
    Guid StatementId,
    Guid FromAccountId,
    DateOnly Date,
    decimal PaidAmount,
    string PaidCurrency,
    decimal CreditedAmount,
    string CreditedCurrency);
