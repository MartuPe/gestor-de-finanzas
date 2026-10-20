using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Transactions.ListTransactions;

public sealed class ListTransactionsHandler(
    ICurrentUser user,
    ITransactionReader transactionReader,
    IValidator<ListTransactionsQuery> validator)
{
    public async Task<Result<TransactionPage>> HandleAsync(ListTransactionsQuery query, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(query, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var filter = new TransactionFilter(
            query.From, query.To, query.AccountId, query.CategoryId, query.TagId, query.Status, query.Page, query.PageSize);
        return await transactionReader.ListAsync(user.LedgerId, filter, cancellationToken);
    }
}
