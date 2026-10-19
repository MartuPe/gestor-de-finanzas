using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.Transactions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Incomes.RegisterIncome;

public sealed class RegisterIncomeHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    ICategoryRepository categories,
    ITagRepository tags,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<RegisterIncomeCommand> validator)
{
    public async Task<Result<Registration<TransactionResponse>>> HandleAsync(
        RegisterIncomeCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var amount = new Money(command.Amount, command.Currency);
        var previous = await Idempotency.CheckAsync(
            transactions,
            command.Id,
            user,
            existing => existing.Kind == TransactionKind.Income
                && existing.AccountId == command.AccountId
                && existing.Date == command.Date
                && existing.Money == amount,
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

        var category = await categories.GetOwnedAsync(command.CategoryId, user, cancellationToken);
        if (category.IsFailure)
        {
            return category.Error;
        }

        var tagList = await tags.GetOwnedAsync(command.TagIds, user, cancellationToken);
        if (tagList.IsFailure)
        {
            return tagList.Error;
        }

        var income = Transaction.CreateIncome(
            command.Id, account.Value, command.Date, amount, category.Value, command.Description);
        if (income.IsFailure)
        {
            return income.Error;
        }

        var tagged = income.Value.SetTags(tagList.Value);
        if (tagged.IsFailure)
        {
            return tagged.Error;
        }

        transactions.Add(income.Value);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<TransactionResponse>(TransactionResponse.From(income.Value), Created: true);
    }
}
