using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;

namespace Finanzas.Application.Transactions.GetTransaction;

/// <summary>Un movimiento completo, con sus cuotas y el mes de vencimiento de cada una.</summary>
public sealed class GetTransactionHandler(
    ICurrentUser user,
    ITransactionRepository transactions,
    IStatementRepository statements)
{
    public async Task<Result<TransactionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await transactions.GetAsync(id, cancellationToken);
        if (transaction is null || transaction.LedgerId != user.LedgerId)
        {
            return ApplicationErrors.TransactionNotFound;
        }

        var statementIds = transaction.Installments.Select(installment => installment.StatementId).Distinct().ToList();
        IReadOnlyList<Statement> related = statementIds.Count == 0
            ? []
            : await statements.GetManyAsync(statementIds, cancellationToken);
        return TransactionResponse.From(transaction, related);
    }
}
