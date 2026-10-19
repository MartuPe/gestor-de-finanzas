using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using FluentValidation;

namespace Finanzas.Application.Recurring.GenerateRecurring;

public sealed class GenerateRecurringHandler(
    ICurrentUser user,
    IRecurringTemplateRepository templates,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IStatementRepository statements,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<GenerateRecurringCommand> validator)
{
    public async Task<Result<GenerateRecurringResponse>> HandleAsync(
        GenerateRecurringCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var active = await templates.ListActiveAsync(user.LedgerId, cancellationToken);
        if (active.Count == 0)
        {
            return new GenerateRecurringResponse(0, []);
        }

        var batch = RecurringGenerator.Generate(
            active,
            command.Period,
            (await accounts.ListAsync(user.LedgerId, cancellationToken)).ToDictionary(account => account.Id),
            (await categories.ListAsync(user.LedgerId, cancellationToken)).ToDictionary(category => category.Id),
            await statements.ListForLedgerFromAsync(user.LedgerId, command.Period, cancellationToken),
            await transactions.GetGeneratedKeysAsync(user.LedgerId, command.Period, cancellationToken));

        var skipped = batch.Skipped
            .Select(item => new SkippedTemplateResponse(item.TemplateId, item.Error.Code, item.Error.Message))
            .ToList();
        if (batch.Transactions.Count == 0)
        {
            return new GenerateRecurringResponse(0, skipped);
        }

        statements.AddRange(batch.NewStatements);
        transactions.AddRange(batch.Transactions);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);

        // Si otro pedido generó lo mismo al mismo tiempo, el UNIQUE lo frena: el resultado es el mismo.
        if (saved.Error == ApplicationErrors.Duplicate)
        {
            return new GenerateRecurringResponse(0, skipped);
        }

        return saved.IsFailure ? saved.Error : new GenerateRecurringResponse(batch.Transactions.Count, skipped);
    }
}
