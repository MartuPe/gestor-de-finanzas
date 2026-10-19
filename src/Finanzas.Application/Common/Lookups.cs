using Finanzas.Application.Abstractions;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Tags;

namespace Finanzas.Application.Common;

/// <summary>
/// Búsquedas acotadas al libro del usuario: lo que es de otro libro se responde como "no encontrado",
/// para no revelar que existe.
/// </summary>
internal static class Lookups
{
    public static async Task<Result<Ledger>> CurrentLedgerAsync(
        this ILedgerRepository ledgers,
        ICurrentUser user,
        CancellationToken cancellationToken) =>
        await ledgers.GetAsync(user.LedgerId, cancellationToken) is { } ledger
            ? ledger
            : ApplicationErrors.LedgerNotFound;

    public static async Task<Result<Account>> GetOwnedAsync(
        this IAccountRepository accounts,
        Guid id,
        ICurrentUser user,
        CancellationToken cancellationToken) =>
        await accounts.GetAsync(id, cancellationToken) is { } account && account.LedgerId == user.LedgerId
            ? account
            : ApplicationErrors.AccountNotFound;

    public static async Task<Result<Category>> GetOwnedAsync(
        this ICategoryRepository categories,
        Guid id,
        ICurrentUser user,
        CancellationToken cancellationToken) =>
        await categories.GetAsync(id, cancellationToken) is { } category && category.LedgerId == user.LedgerId
            ? category
            : ApplicationErrors.CategoryNotFound;

    public static async Task<Result<IReadOnlyList<Tag>>> GetOwnedAsync(
        this ITagRepository tags,
        IReadOnlyCollection<Guid>? ids,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0)
        {
            return Result<IReadOnlyList<Tag>>.Success([]);
        }

        var distinct = ids.Distinct().ToList();
        var found = await tags.GetManyAsync(distinct, cancellationToken);
        return found.Count == distinct.Count && found.All(tag => tag.LedgerId == user.LedgerId)
            ? Result<IReadOnlyList<Tag>>.Success(found)
            : ApplicationErrors.TagNotFound;
    }
}
