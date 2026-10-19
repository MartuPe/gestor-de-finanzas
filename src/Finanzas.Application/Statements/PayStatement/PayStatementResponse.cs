using Finanzas.Application.Transactions;
using Finanzas.Domain.Statements;

namespace Finanzas.Application.Statements.PayStatement;

/// <param name="Outstanding">Lo que queda por pagar del resumen, por moneda.</param>
public sealed record PayStatementResponse(
    TransactionResponse Out,
    TransactionResponse In,
    StatementStatus Status,
    IReadOnlyDictionary<string, decimal> Outstanding);
