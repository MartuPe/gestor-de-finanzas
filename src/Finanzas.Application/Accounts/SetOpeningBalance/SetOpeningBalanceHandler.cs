using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.Transactions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Accounts.SetOpeningBalance;

public sealed class SetOpeningBalanceHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<SetOpeningBalanceCommand> validator)
{
    public async Task<Result<Registration<TransactionResponse>>> HandleAsync(
        SetOpeningBalanceCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var balance = new Money(command.Amount, command.Currency);
        var previous = await Idempotency.CheckAsync(
            transactions,
            command.TransactionId,
            user,
            existing => existing.Kind == TransactionKind.Opening
                && existing.AccountId == command.AccountId
                && existing.Money == balance,
            cancellationToken);
        if (previous is not null)
        {
            return previous.IsFailure
                ? previous.Error
                : new Registration<TransactionResponse>(TransactionResponse.From(previous.Value), Created: false);
        }

        var account = await accounts.GetOwnedAsync(command.AccountId, user, cancellationToken);
        if (account.IsFailure)
        {
            return account.Error;
        }

        if (await transactions.HasOpeningAsync(account.Value.Id, command.Currency, cancellationToken))
        {
            return ApplicationErrors.OpeningExists;
        }

        var opening = Transaction.CreateOpening(command.TransactionId, account.Value, command.Date, balance);
        if (opening.IsFailure)
        {
            return opening.Error;
        }

        transactions.Add(opening.Value);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<TransactionResponse>(TransactionResponse.From(opening.Value), Created: true);
    }
}
