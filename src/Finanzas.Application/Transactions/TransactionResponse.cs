using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;

namespace Finanzas.Application.Transactions;

public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    DateOnly Date,
    decimal Amount,
    string Currency,
    TransactionKind Kind,
    TransactionStatus Status,
    Guid? CategoryId,
    string? Description,
    Guid? TransferGroupId,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<InstallmentResponse> Installments)
{
    /// <param name="statements">Los resúmenes de las cuotas, para informar en qué mes vence cada una.</param>
    public static TransactionResponse From(Transaction transaction, IEnumerable<Statement>? statements = null)
    {
        var byId = (statements ?? []).DistinctBy(statement => statement.Id).ToDictionary(statement => statement.Id);
        var installments = transaction.Installments
            .OrderBy(installment => installment.Number)
            .Select(installment =>
            {
                var statement = byId.GetValueOrDefault(installment.StatementId);
                return new InstallmentResponse(
                    installment.Number,
                    installment.Total,
                    installment.Amount,
                    installment.Currency,
                    installment.StatementId,
                    statement?.Period,
                    statement?.DueDate);
            })
            .ToList();

        return new TransactionResponse(
            transaction.Id,
            transaction.AccountId,
            transaction.Date,
            transaction.Amount,
            transaction.Currency,
            transaction.Kind,
            transaction.Status,
            transaction.CategoryId,
            transaction.Description,
            transaction.TransferGroupId,
            transaction.TagIds,
            installments);
    }
}

/// <param name="StatementPeriod">Mes de vencimiento del resumen en el que cae la cuota.</param>
public sealed record InstallmentResponse(
    int Number,
    int Total,
    decimal Amount,
    string Currency,
    Guid StatementId,
    Period? StatementPeriod,
    DateOnly? DueDate);
