using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Accounts.ListAccounts;

/// <summary>Las cuentas del libro con su saldo por moneda (R1), incluidas las archivadas.</summary>
public sealed class ListAccountsHandler(ICurrentUser user, IAccountRepository accounts, IAccountReader accountReader)
{
    public async Task<Result<IReadOnlyList<AccountResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var all = await accounts.ListAsync(user.LedgerId, cancellationToken);
        var balances = await accountReader.GetBalancesAsync(user.LedgerId, cancellationToken);

        return Result<IReadOnlyList<AccountResponse>>.Success(all
            .OrderBy(account => account.IsArchived)
            .ThenBy(account => account.SortOrder)
            .ThenBy(account => account.Name, StringComparer.CurrentCulture)
            .Select(account => AccountResponse.From(account, balances.GetValueOrDefault(account.Id)))
            .ToList());
    }
}
