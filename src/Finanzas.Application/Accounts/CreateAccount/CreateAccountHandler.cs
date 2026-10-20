using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Accounts.CreateAccount;

public sealed class CreateAccountHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IValidator<CreateAccountCommand> validator)
{
    public async Task<Result<Registration<AccountResponse>>> HandleAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await AccountCreation.PreviousAsync(accounts, command.Id, command.Name, user, cancellationToken) is { } previous)
        {
            return previous;
        }

        var account = Account.Create(
            command.Id, user.LedgerId, command.Name, command.Kind, command.DefaultCurrency, command.IsLiquid, command.EarnsYield);
        if (account.IsFailure)
        {
            return account.Error;
        }

        return await AccountCreation.SaveAsync(account.Value, command.Color, command.Icon, accounts, unitOfWork, cancellationToken);
    }
}
