using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Accounts;

/// <summary>Lo que comparten el alta de una cuenta de dinero y la de una de crédito.</summary>
internal static class AccountCreation
{
    /// <summary>
    /// null si se puede crear. Si no, lo que hay que responder: la misma cuenta (el pedido se repitió),
    /// un Id usado en otro libro o un nombre repetido.
    /// </summary>
    public static async Task<Result<Registration<AccountResponse>>?> PreviousAsync(
        IAccountRepository accounts,
        Guid id,
        string name,
        ICurrentUser user,
        CancellationToken cancellationToken)
    {
        if (await accounts.GetAsync(id, cancellationToken) is { } existing)
        {
            return existing.LedgerId == user.LedgerId
                ? Result<Registration<AccountResponse>>.Success(new(AccountResponse.From(existing), Created: false))
                : Result<Registration<AccountResponse>>.Failure(ApplicationErrors.IdInUse);
        }

        var all = await accounts.ListAsync(user.LedgerId, cancellationToken);
        return all.Any(account => string.Equals(account.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
            ? Result<Registration<AccountResponse>>.Failure(ApplicationErrors.AccountNameTaken)
            : null;
    }

    public static async Task<Result<Registration<AccountResponse>>> SaveAsync(
        Account account,
        string? color,
        string? icon,
        IAccountRepository accounts,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var appearance = account.ChangeAppearance(color, icon);
        if (appearance.IsFailure)
        {
            return appearance.Error;
        }

        accounts.Add(account);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<AccountResponse>(AccountResponse.From(account), Created: true);
    }
}
