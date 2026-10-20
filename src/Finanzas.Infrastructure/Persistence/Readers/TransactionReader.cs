using Finanzas.Application.Abstractions;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Readers;

internal sealed class TransactionReader(FinanzasDbContext db) : ITransactionReader
{
    public async Task<TransactionPage> ListAsync(Guid ledgerId, TransactionFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Transactions.AsNoTracking()
            .Where(transaction => transaction.LedgerId == ledgerId
                && transaction.Date >= filter.From
                && transaction.Date <= filter.To);

        if (filter.AccountId is Guid accountId)
        {
            query = query.Where(transaction => transaction.AccountId == accountId);
        }

        if (filter.CategoryId is Guid categoryId)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId);
        }

        if (filter.TagId is Guid tagId)
        {
            query = query.Where(transaction => db.Set<TransactionTag>()
                .Any(link => link.TransactionId == transaction.Id && link.TagId == tagId));
        }

        if (filter.Status is TransactionStatus status)
        {
            query = query.Where(transaction => transaction.Status == status);
        }

        var total = await query.CountAsync(cancellationToken);

        // Del más nuevo al más viejo; con la misma fecha, el último cargado primero (los Id son secuenciales).
        var page = await (
                from transaction in query
                join account in db.Accounts on transaction.AccountId equals account.Id
                join category in db.Categories on transaction.CategoryId equals category.Id into categories
                from category in categories.DefaultIfEmpty()
                orderby transaction.Date descending, transaction.Id descending
                select new
                {
                    transaction.Id,
                    transaction.Date,
                    transaction.AccountId,
                    AccountName = account.Name,
                    transaction.Kind,
                    transaction.Status,
                    transaction.Amount,
                    transaction.Currency,
                    transaction.CategoryId,
                    CategoryName = category == null ? null : category.Name,
                    transaction.Description,
                    transaction.TransferGroupId,
                    Installments = db.Installments.Count(installment => installment.TransactionId == transaction.Id),
                })
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var ids = page.Select(item => item.Id).ToList();
        var tags = (await db.Set<TransactionTag>().AsNoTracking()
                .Where(link => ids.Contains(link.TransactionId))
                .Select(link => new { link.TransactionId, link.TagId })
                .ToListAsync(cancellationToken))
            .ToLookup(link => link.TransactionId, link => link.TagId);

        var items = page
            .Select(item => new TransactionListItem(
                item.Id,
                item.Date,
                item.AccountId,
                item.AccountName,
                item.Kind,
                item.Status,
                item.Amount,
                item.Currency,
                item.CategoryId,
                item.CategoryName,
                item.Description,
                item.TransferGroupId,
                tags[item.Id].ToList(),
                item.Installments == 0 ? null : item.Installments))
            .ToList();

        return new TransactionPage(items, total);
    }
}
