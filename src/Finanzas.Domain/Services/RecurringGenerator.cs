using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;

namespace Finanzas.Domain.Services;

/// <summary>Clave que hace idempotente la generación: coincide con el UNIQUE de la base (01-modelo §4.10).</summary>
public sealed record GeneratedKey(Guid TemplateId, Period Period, Guid AccountId)
{
    public static GeneratedKey? Of(Transaction transaction) =>
        transaction is { RecurringTemplateId: Guid templateId, RecurringPeriod: Period period }
            ? new GeneratedKey(templateId, period, transaction.AccountId)
            : null;
}

public sealed record SkippedTemplate(Guid TemplateId, Error Error);

/// <param name="NewStatements">Resúmenes estimados creados para los fijos en cuentas de crédito.</param>
/// <param name="Skipped">Plantillas que no se pudieron generar; las demás se generan igual.</param>
public sealed record RecurringBatch(
    IReadOnlyList<Transaction> Transactions,
    IReadOnlyList<Statement> NewStatements,
    IReadOnlyList<SkippedTemplate> Skipped);

/// <summary>
/// R7: genera los movimientos previstos de un mes a partir de las plantillas.
/// Lo que ya se generó (aunque después se haya borrado) no se vuelve a generar.
/// </summary>
public static class RecurringGenerator
{
    public static RecurringBatch Generate(
        IEnumerable<RecurringTemplate> templates,
        Period period,
        IReadOnlyDictionary<Guid, Account> accounts,
        IReadOnlyDictionary<Guid, Category> categories,
        IEnumerable<Statement> statements,
        IReadOnlySet<GeneratedKey> alreadyGenerated)
    {
        var statementList = statements.ToList();
        var books = new Dictionary<Guid, StatementBook>();
        var transactions = new List<Transaction>();
        var skipped = new List<SkippedTemplate>();

        foreach (var template in templates.Where(template => template.OccursIn(period)))
        {
            if (alreadyGenerated.Contains(new GeneratedKey(template.Id, period, template.AccountId)))
            {
                continue;
            }

            var generated = GenerateOne(template, period, accounts, categories, statementList, books);
            if (generated.IsFailure)
            {
                skipped.Add(new SkippedTemplate(template.Id, generated.Error));
                continue;
            }

            foreach (var transaction in generated.Value)
            {
                transaction.MarkGeneratedBy(template, period);
                transactions.Add(transaction);
            }
        }

        var newStatements = books.Values.SelectMany(book => book.Created).ToList();
        return new RecurringBatch(transactions, newStatements, skipped);
    }

    private static Result<IReadOnlyList<Transaction>> GenerateOne(
        RecurringTemplate template,
        Period period,
        IReadOnlyDictionary<Guid, Account> accounts,
        IReadOnlyDictionary<Guid, Category> categories,
        List<Statement> statements,
        Dictionary<Guid, StatementBook> books)
    {
        if (!accounts.TryGetValue(template.AccountId, out var account))
        {
            return RecurringErrors.AccountNotFound;
        }

        var date = template.OccurrenceDate(period);
        var amount = template.EstimatedMoney;

        if (template.Kind == RecurringKind.Transfer)
        {
            if (template.ToAccountId is not Guid toAccountId || !accounts.TryGetValue(toAccountId, out var toAccount))
            {
                return RecurringErrors.AccountNotFound;
            }

            var legs = TransferFactory.CreateTransfer(
                SequentialGuid.New(), SequentialGuid.New(), account, toAccount, date, amount, amount, template.Description, TransactionStatus.Planned);
            return legs.IsSuccess
                ? Result<IReadOnlyList<Transaction>>.Success([legs.Value.Out, legs.Value.In])
                : legs.Error;
        }

        if (template.CategoryId is not Guid categoryId || !categories.TryGetValue(categoryId, out var category))
        {
            return RecurringErrors.CategoryNotFound;
        }

        if (template.Kind == RecurringKind.Income)
        {
            var income = Transaction.CreateIncome(
                SequentialGuid.New(), account, date, amount, category, template.Description, TransactionStatus.Planned);
            return income.IsSuccess ? Result<IReadOnlyList<Transaction>>.Success([income.Value]) : income.Error;
        }

        InstallmentPlan? plan = null;
        if (account.IsCredit)
        {
            if (!books.TryGetValue(account.Id, out var book))
            {
                book = new StatementBook(account, statements);
                books.Add(account.Id, book);
            }

            var planned = InstallmentScheduler.Plan(book, account, date, amount, installments: 1);
            if (planned.IsFailure)
            {
                return planned.Error;
            }

            plan = planned.Value;
        }

        var expense = Transaction.CreateExpense(
            SequentialGuid.New(), account, date, amount, category, template.Description, plan, TransactionStatus.Planned);
        return expense.IsSuccess ? Result<IReadOnlyList<Transaction>>.Success([expense.Value]) : expense.Error;
    }
}
