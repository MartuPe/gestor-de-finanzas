using Finanzas.Application.Abstractions;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Services;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Tags;
using Finanzas.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Repositories;

internal sealed class LedgerRepository(FinanzasDbContext db) : ILedgerRepository
{
    public Task<Ledger?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Ledgers.SingleOrDefaultAsync(ledger => ledger.Id == id, cancellationToken);
}

internal sealed class AccountRepository(FinanzasDbContext db) : IAccountRepository
{
    public Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Accounts.SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListAsync(Guid ledgerId, CancellationToken cancellationToken) =>
        await db.Accounts.Where(account => account.LedgerId == ledgerId).ToListAsync(cancellationToken);
}

internal sealed class CategoryRepository(FinanzasDbContext db) : ICategoryRepository
{
    public Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(Guid ledgerId, CancellationToken cancellationToken) =>
        await db.Categories.Where(category => category.LedgerId == ledgerId).ToListAsync(cancellationToken);
}

internal sealed class TagRepository(FinanzasDbContext db) : ITagRepository
{
    public async Task<IReadOnlyList<Tag>> ListAsync(Guid ledgerId, CancellationToken cancellationToken) =>
        await db.Tags.Where(tag => tag.LedgerId == ledgerId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Tags.Where(tag => ids.Contains(tag.Id)).ToListAsync(cancellationToken);
}

internal sealed class StatementRepository(FinanzasDbContext db) : IStatementRepository
{
    public Task<Statement?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Statements.SingleOrDefaultAsync(statement => statement.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Statement>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Statements.Where(statement => ids.Contains(statement.Id)).ToListAsync(cancellationToken);

    // El vencimiento cae dentro del período: comparar por DueDate evita comparar Period convertido en SQL.
    public async Task<IReadOnlyList<Statement>> ListFromAsync(Guid creditAccountId, Period from, CancellationToken cancellationToken) =>
        await db.Statements
            .Where(statement => statement.CreditAccountId == creditAccountId && statement.DueDate >= from.FirstDay)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Statement>> ListForLedgerFromAsync(Guid ledgerId, Period from, CancellationToken cancellationToken)
    {
        var firstDay = from.FirstDay;
        return await (
                from statement in db.Statements
                join account in db.Accounts on statement.CreditAccountId equals account.Id
                where account.LedgerId == ledgerId && statement.DueDate >= firstDay
                select statement)
            .ToListAsync(cancellationToken);
    }

    public void AddRange(IEnumerable<Statement> statements) => db.Statements.AddRange(statements);
}

internal sealed class TransactionRepository(FinanzasDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Transactions.SingleOrDefaultAsync(transaction => transaction.Id == id, cancellationToken);

    public Task<bool> IdInUseAsync(Guid id, CancellationToken cancellationToken) =>
        db.Transactions.IgnoreQueryFilters().AnyAsync(transaction => transaction.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTransferLegsAsync(Guid transferGroupId, CancellationToken cancellationToken) =>
        await db.Transactions.Where(transaction => transaction.TransferGroupId == transferGroupId).ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<GeneratedKey>> GetGeneratedKeysAsync(Guid ledgerId, Period period, CancellationToken cancellationToken)
    {
        // Sin el filtro de borrado lógico: un fijo borrado no se vuelve a generar ese mes.
        var keys = await db.Transactions
            .IgnoreQueryFilters()
            .Where(transaction => transaction.LedgerId == ledgerId && transaction.RecurringPeriod == period)
            .Select(transaction => new { transaction.RecurringTemplateId, transaction.AccountId })
            .ToListAsync(cancellationToken);

        return keys
            .Where(key => key.RecurringTemplateId is not null)
            .Select(key => new GeneratedKey(key.RecurringTemplateId!.Value, period, key.AccountId))
            .ToHashSet();
    }

    public void Add(Transaction transaction) => db.Transactions.Add(transaction);

    public void AddRange(IEnumerable<Transaction> transactions) => db.Transactions.AddRange(transactions);
}

internal sealed class RecurringTemplateRepository(FinanzasDbContext db) : IRecurringTemplateRepository
{
    public async Task<IReadOnlyList<RecurringTemplate>> ListActiveAsync(Guid ledgerId, CancellationToken cancellationToken) =>
        await db.RecurringTemplates
            .Where(template => template.LedgerId == ledgerId && template.IsActive)
            .ToListAsync(cancellationToken);
}
