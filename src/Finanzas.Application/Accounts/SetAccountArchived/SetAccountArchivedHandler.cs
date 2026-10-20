using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Accounts.SetAccountArchived;

public sealed class SetAccountArchivedHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<AccountResponse>> HandleAsync(SetAccountArchivedCommand command, CancellationToken cancellationToken)
    {
        var account = await accounts.GetOwnedAsync(command.Id, user, cancellationToken);
        if (account.IsFailure)
        {
            return account.Error;
        }

        if (command.Archived)
        {
            account.Value.Archive(timeProvider.GetUtcNow());
        }
        else
        {
            account.Value.Unarchive();
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error : AccountResponse.From(account.Value);
    }
}
