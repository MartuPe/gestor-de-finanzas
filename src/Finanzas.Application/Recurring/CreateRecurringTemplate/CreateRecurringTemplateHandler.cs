using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;
using FluentValidation;

namespace Finanzas.Application.Recurring.CreateRecurringTemplate;

public sealed class CreateRecurringTemplateHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IRecurringTemplateRepository templates,
    IUnitOfWork unitOfWork,
    IValidator<CreateRecurringTemplateCommand> validator)
{
    public async Task<Result<Registration<RecurringTemplateResponse>>> HandleAsync(
        CreateRecurringTemplateCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await templates.GetAsync(command.Id, cancellationToken) is { } existing)
        {
            return existing.LedgerId == user.LedgerId
                ? new Registration<RecurringTemplateResponse>(RecurringTemplateResponse.From(existing), Created: false)
                : ApplicationErrors.IdInUse;
        }

        var account = await accounts.GetOwnedAsync(command.AccountId, user, cancellationToken);
        if (account.IsFailure)
        {
            return account.Error;
        }

        Account? toAccount = null;
        if (command.ToAccountId is Guid toAccountId)
        {
            var found = await accounts.GetOwnedAsync(toAccountId, user, cancellationToken);
            if (found.IsFailure)
            {
                return found.Error;
            }

            toAccount = found.Value;
        }

        Category? category = null;
        if (command.CategoryId is Guid categoryId)
        {
            var found = await categories.GetOwnedAsync(categoryId, user, cancellationToken);
            if (found.IsFailure)
            {
                return found.Error;
            }

            category = found.Value;
        }

        var template = RecurringTemplate.Create(
            command.Id,
            command.Description,
            command.Kind,
            account.Value,
            toAccount,
            category,
            new Money(command.EstimatedAmount, command.Currency),
            command.Frequency,
            command.Interval,
            command.DayOfMonth,
            command.StartsOn,
            command.EndsOn);
        if (template.IsFailure)
        {
            return template.Error;
        }

        templates.Add(template.Value);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<RecurringTemplateResponse>(RecurringTemplateResponse.From(template.Value), Created: true);
    }
}
