using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Services;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Tags;
using Finanzas.Domain.Transactions;

namespace Finanzas.Application.Abstractions;

// Repositorios del lado de escritura: devuelven agregados completos y con seguimiento de cambios.
// Solo tienen los métodos que usan los casos de uso.

public interface ILedgerRepository
{
    Task<Ledger?> GetAsync(Guid id, CancellationToken cancellationToken);

    void Add(Ledger ledger);
}

public interface IAccountRepository
{
    Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Todas las cuentas del libro, incluidas las archivadas.</summary>
    Task<IReadOnlyList<Account>> ListAsync(Guid ledgerId, CancellationToken cancellationToken);

    void Add(Account account);
}

public interface ICategoryRepository
{
    Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> ListAsync(Guid ledgerId, CancellationToken cancellationToken);

    void AddRange(IEnumerable<Category> categories);
}

public interface ITagRepository
{
    Task<Tag?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Tag>> ListAsync(Guid ledgerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Tag>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(Tag tag);
}

public interface IStatementRepository
{
    Task<Statement?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Statement>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Los resúmenes de una línea de crédito que vencen en <paramref name="from"/> o después.</summary>
    Task<IReadOnlyList<Statement>> ListFromAsync(Guid creditAccountId, Period from, CancellationToken cancellationToken);

    /// <summary>Lo mismo, para todas las líneas de crédito del libro.</summary>
    Task<IReadOnlyList<Statement>> ListForLedgerFromAsync(Guid ledgerId, Period from, CancellationToken cancellationToken);

    void AddRange(IEnumerable<Statement> statements);
}

public interface ITransactionRepository
{
    Task<Transaction?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Si el Id ya existe, aunque el movimiento esté borrado: un Id no se reutiliza.</summary>
    Task<bool> IdInUseAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetTransferLegsAsync(Guid transferGroupId, CancellationToken cancellationToken);

    /// <summary>R7: lo que ya se generó para ese período, incluidos los movimientos borrados.</summary>
    Task<IReadOnlySet<GeneratedKey>> GetGeneratedKeysAsync(Guid ledgerId, Period period, CancellationToken cancellationToken);

    /// <summary>Si la cuenta ya tiene saldo inicial en esa moneda (hay uno solo por cuenta y moneda).</summary>
    Task<bool> HasOpeningAsync(Guid accountId, string currency, CancellationToken cancellationToken);

    void Add(Transaction transaction);

    void AddRange(IEnumerable<Transaction> transactions);
}

public interface IRecurringTemplateRepository
{
    Task<RecurringTemplate?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecurringTemplate>> ListAsync(Guid ledgerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecurringTemplate>> ListActiveAsync(Guid ledgerId, CancellationToken cancellationToken);

    void Add(RecurringTemplate template);
}
