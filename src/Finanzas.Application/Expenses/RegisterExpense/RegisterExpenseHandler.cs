using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.Transactions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Expenses.RegisterExpense;

public sealed class RegisterExpenseHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    ICategoryRepository categories,
    ITagRepository tags,
    IStatementRepository statements,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<RegisterExpenseCommand> validator)
{
    public async Task<Result<Registration<TransactionResponse>>> HandleAsync(
        RegisterExpenseCommand command,
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
            existing => existing.Kind == TransactionKind.Expense
                && existing.AccountId == command.AccountId
                && existing.Date == command.Date
                && existing.Money == amount.Negate(),
            cancellationToken);
        if (previous is not null)
        {
            return previous.IsFailure ? previous.Error : await ReplayAsync(previous.Value, cancellationToken);
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

        InstallmentPlan? plan = null;
        if (account.Value.IsCredit)
        {
            // R4: la primera cuota no puede caer en un resumen que venza antes del mes de la compra.
            var existing = await statements.ListFromAsync(account.Value.Id, Period.FromDate(command.Date), cancellationToken);
            var planned = InstallmentScheduler.Plan(account.Value, command.Date, amount, command.Installments, existing);
            if (planned.IsFailure)
            {
                return planned.Error;
            }

            plan = planned.Value;
        }
        else if (command.Installments != 1)
        {
            return TransactionErrors.PlanOnlyForCredit;
        }

        var expense = Transaction.CreateExpense(
            command.Id, account.Value, command.Date, amount, category.Value, command.Description, plan);
        if (expense.IsFailure)
        {
            return expense.Error;
        }

        var tagged = expense.Value.SetTags(tagList.Value);
        if (tagged.IsFailure)
        {
            return tagged.Error;
        }

        if (plan is not null)
        {
            statements.AddRange(plan.NewStatements);
        }

        transactions.Add(expense.Value);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var response = TransactionResponse.From(expense.Value, plan?.Installments.Select(installment => installment.Statement));
        return new Registration<TransactionResponse>(response, Created: true);
    }

    private async Task<Registration<TransactionResponse>> ReplayAsync(Transaction existing, CancellationToken cancellationToken)
    {
        var statementIds = existing.Installments.Select(installment => installment.StatementId).Distinct().ToList();
        IReadOnlyList<Statement> related = statementIds.Count == 0
            ? []
            : await statements.GetManyAsync(statementIds, cancellationToken);
        return new Registration<TransactionResponse>(TransactionResponse.From(existing, related), Created: false);
    }
}
