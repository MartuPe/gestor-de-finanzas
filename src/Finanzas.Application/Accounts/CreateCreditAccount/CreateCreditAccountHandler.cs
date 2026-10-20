using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Accounts.CreateCreditAccount;

public sealed class CreateCreditAccountHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    IUnitOfWork unitOfWork,
    IValidator<CreateCreditAccountCommand> validator)
{
    public async Task<Result<Registration<AccountResponse>>> HandleAsync(
        CreateCreditAccountCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await AccountCreation.PreviousAsync(accounts, command.Id, command.Name, user, cancellationToken) is { } previous)
        {
            return previous;
        }

        var account = Account.CreateCredit(
            command.Id,
            user.LedgerId,
            command.Name,
            command.DefaultCurrency,
            command.LineType,
            command.Issuer,
            command.Network,
            command.UsualClosingDay,
            command.UsualDueDay);
        if (account.IsFailure)
        {
            return account.Error;
        }

        return await AccountCreation.SaveAsync(account.Value, command.Color, command.Icon, accounts, unitOfWork, cancellationToken);
    }
}
