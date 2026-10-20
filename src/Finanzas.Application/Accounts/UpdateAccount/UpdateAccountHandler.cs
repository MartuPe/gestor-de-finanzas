using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Accounts.UpdateAccount;

public sealed class UpdateAccountHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IValidator<UpdateAccountCommand> validator)
{
    public async Task<Result<AccountResponse>> HandleAsync(UpdateAccountCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var account = await accounts.GetOwnedAsync(command.Id, user, cancellationToken);
        if (account.IsFailure)
        {
            return account.Error;
        }

        var others = await accounts.ListAsync(user.LedgerId, cancellationToken);
        if (others.Any(other => other.Id != command.Id
            && string.Equals(other.Name, command.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationErrors.AccountNameTaken;
        }

        var result = account.Value.Rename(command.Name);
        if (result.IsSuccess)
        {
            result = account.Value.ChangeAppearance(command.Color, command.Icon);
        }

        if (result.IsSuccess && command.UsualDueDay is int dueDay)
        {
            result = account.Value.CreditLine is { } line
                ? line.ChangeUsualDays(command.UsualClosingDay, dueDay)
                : ApplicationErrors.NotCreditAccount;
        }

        if (result.IsFailure)
        {
            return result.Error;
        }

        account.Value.ChangeSortOrder(command.SortOrder);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error : AccountResponse.From(account.Value);
    }
}
