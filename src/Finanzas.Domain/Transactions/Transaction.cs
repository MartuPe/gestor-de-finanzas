using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Tags;

namespace Finanzas.Domain.Transactions;

/// <summary>
/// Un movimiento de plata en una cuenta. El monto lleva signo,
/// así que el saldo de una cuenta es la suma de sus movimientos (R1).
/// </summary>
public sealed class Transaction : Entity, IAggregateRoot
{
    public const int DescriptionMaxLength = 200;

    private readonly List<Installment> _installments = [];
    private readonly List<TransactionTag> _tags = [];

    private Transaction()
    {
    }

    private Transaction(
        Guid id,
        Account account,
        DateOnly date,
        Money amount,
        TransactionKind kind,
        TransactionStatus status,
        Guid? categoryId,
        string? description)
        : base(id)
    {
        LedgerId = account.LedgerId;
        AccountId = account.Id;
        Date = date;
        Amount = amount.Amount;
        Currency = amount.Currency;
        Kind = kind;
        Status = status;
        CategoryId = categoryId;
        Description = Text.Clean(description);
    }

    public Guid LedgerId { get; private set; }

    /// <summary>El medio: la cuenta de donde sale o adonde entra la plata.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Fecha del consumo o del movimiento.</summary>
    public DateOnly Date { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public TransactionKind Kind { get; private set; }

    public TransactionStatus Status { get; private set; }

    public Guid? CategoryId { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Une las dos patas de una transferencia o de un pago de resumen.</summary>
    public Guid? TransferGroupId { get; private set; }

    public Guid? RecurringTemplateId { get; private set; }

    public Period? RecurringPeriod { get; private set; }

    public Guid? PaidStatementId { get; private set; }

    public Money Money => new(Amount, Currency);

    public bool IsPlanned => Status == TransactionStatus.Planned;

    public bool IsTransferLeg => TransferGroupId is not null;

    public bool IsCreditPurchase => _installments.Count > 0;

    public IReadOnlyList<Installment> Installments => _installments;

    public IReadOnlyList<Guid> TagIds => _tags.ConvertAll(tag => tag.TagId);

    /// <summary>
    /// Un gasto. <paramref name="amount"/> va en positivo y se guarda con signo negativo.
    /// En una cuenta de crédito necesita el plan de cuotas que calcula <c>InstallmentScheduler</c>.
    /// </summary>
    public static Result<Transaction> CreateExpense(
        Guid id,
        Account account,
        DateOnly date,
        Money amount,
        Category category,
        string? description = null,
        InstallmentPlan? plan = null,
        TransactionStatus status = TransactionStatus.Confirmed)
    {
        var error = ValidatePositive(amount)
            ?? ValidateAccount(account)
            ?? ValidateDescription(description)
            ?? ValidateCategory(account, category, CategoryKind.Expense)
            ?? ValidatePlan(account, amount, plan);
        if (error is not null)
        {
            return error;
        }

        var transaction = new Transaction(
            id, account, date, amount.Negate(), TransactionKind.Expense, status, category.Id, description);
        transaction.AddInstallments(plan);
        return transaction;
    }

    /// <summary>
    /// Una devolución o un reintegro: descuenta del gasto de su categoría.
    /// En una cuenta de crédito va como una cuota negativa en un resumen.
    /// </summary>
    public static Result<Transaction> CreateRefund(
        Guid id,
        Account account,
        DateOnly date,
        Money amount,
        Category category,
        string? description = null,
        InstallmentPlan? plan = null)
    {
        var error = ValidatePositive(amount)
            ?? ValidateAccount(account)
            ?? ValidateDescription(description)
            ?? ValidateCategory(account, category, CategoryKind.Expense)
            ?? ValidatePlan(account, amount.Negate(), plan);
        if (error is not null)
        {
            return error;
        }

        var transaction = new Transaction(
            id, account, date, amount, TransactionKind.Refund, TransactionStatus.Confirmed, category.Id, description);
        transaction.AddInstallments(plan);
        return transaction;
    }

    public static Result<Transaction> CreateIncome(
        Guid id,
        Account account,
        DateOnly date,
        Money amount,
        Category category,
        string? description = null,
        TransactionStatus status = TransactionStatus.Confirmed)
    {
        var error = ValidatePositive(amount)
            ?? ValidateAccount(account)
            ?? ValidateDescription(description)
            ?? (account.IsCredit ? TransactionErrors.IncomeOnCredit : null)
            ?? ValidateCategory(account, category, CategoryKind.Income);
        if (error is not null)
        {
            return error;
        }

        return new Transaction(id, account, date, amount, TransactionKind.Income, status, category.Id, description);
    }

    /// <summary>R11: los rendimientos van solo en cuentas que generan intereses.</summary>
    public static Result<Transaction> CreateYield(
        Guid id,
        Account account,
        DateOnly date,
        Money amount,
        Category? category = null,
        string? description = null)
    {
        var error = ValidatePositive(amount)
            ?? ValidateAccount(account)
            ?? ValidateDescription(description)
            ?? (account.EarnsYield ? null : TransactionErrors.AccountDoesNotEarnYield)
            ?? (category is null ? null : ValidateCategory(account, category, CategoryKind.Income));
        if (error is not null)
        {
            return error;
        }

        return new Transaction(
            id, account, date, amount, TransactionKind.Yield, TransactionStatus.Confirmed, category?.Id, description);
    }

    /// <summary>Saldo inicial de una cuenta: uno por cuenta y moneda.</summary>
    public static Result<Transaction> CreateOpening(Guid id, Account account, DateOnly date, Money balance)
    {
        var error = (balance.IsZero ? TransactionErrors.AmountRequired : null) ?? ValidateAccount(account);
        if (error is not null)
        {
            return error;
        }

        return new Transaction(
            id, account, date, balance, TransactionKind.Opening, TransactionStatus.Confirmed, categoryId: null, "Saldo inicial");
    }

    /// <summary>
    /// R12: registra la diferencia entre el saldo real y el calculado, como el "faltante" del Excel.
    /// </summary>
    public static Result<Transaction> CreateReconciliation(
        Guid id,
        Account account,
        DateOnly date,
        Money calculatedBalance,
        Money realBalance,
        Category adjustmentCategory,
        string? description = null)
    {
        if (calculatedBalance.Currency != realBalance.Currency)
        {
            return TransactionErrors.CurrencyMismatch;
        }

        var difference = realBalance - calculatedBalance;
        var error = (difference.IsZero ? TransactionErrors.NothingToAdjust : null)
            ?? ValidateAccount(account)
            ?? ValidateDescription(description)
            ?? (adjustmentCategory.LedgerId == account.LedgerId ? null : TransactionErrors.CategoryFromAnotherLedger);
        if (error is not null)
        {
            return error;
        }

        return new Transaction(
            id,
            account,
            date,
            difference,
            TransactionKind.Adjustment,
            TransactionStatus.Confirmed,
            adjustmentCategory.Id,
            description ?? "Ajuste de saldo");
    }

    /// <summary>R7: confirma un previsto con el monto y la fecha reales (por ejemplo, la luz vino más cara).</summary>
    public Result Confirm(Money? actualAmount = null, DateOnly? actualDate = null)
    {
        if (!IsPlanned)
        {
            return TransactionErrors.AlreadyConfirmed;
        }

        if (IsTransferLeg)
        {
            return TransactionErrors.ConfirmTransferTogether;
        }

        if (actualDate is DateOnly date && date != Date && IsCreditPurchase)
        {
            return TransactionErrors.CannotMoveCreditPurchase;
        }

        if (actualAmount is Money actual)
        {
            if (actual.Currency != Currency)
            {
                return TransactionErrors.CurrencyMismatch;
            }

            if (!actual.IsPositive)
            {
                return TransactionErrors.AmountMustBePositive;
            }

            if (_installments.Count > 1)
            {
                return TransactionErrors.UseEditInstallments;
            }

            var signed = Amount < 0 ? -actual.Amount : actual.Amount;
            if (_installments.Count == 1)
            {
                _installments[0].ChangeAmount(-signed);
            }

            Amount = signed;
        }

        if (actualDate is DateOnly newDate)
        {
            Date = newDate;
        }

        Status = TransactionStatus.Confirmed;
        return Result.Success();
    }

    /// <summary>
    /// R3: las cuotas se pueden editar una por una, porque no siempre son iguales
    /// ("Perfume mamá": 30.000 / 30.000 / 31.600). El total de la compra se recalcula.
    /// </summary>
    public Result EditInstallmentAmounts(IReadOnlyList<decimal> amounts)
    {
        if (!IsCreditPurchase)
        {
            return TransactionErrors.NotACreditPurchase;
        }

        if (amounts.Count != _installments.Count)
        {
            return TransactionErrors.InstallmentCountMismatch;
        }

        var expectedSign = Kind == TransactionKind.Expense ? 1 : -1;
        if (amounts.Any(amount => Math.Sign(amount) != expectedSign || decimal.Round(amount, 2) != amount))
        {
            return TransactionErrors.InvalidInstallmentAmount;
        }

        var ordered = _installments.OrderBy(installment => installment.Number).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].ChangeAmount(amounts[i]);
        }

        Amount = -amounts.Sum();
        return Result.Success();
    }

    public Result SetTags(IEnumerable<Tag> tags)
    {
        var list = tags.ToList();
        if (list.Exists(tag => tag.LedgerId != LedgerId))
        {
            return TransactionErrors.TagFromAnotherLedger;
        }

        // Se aplican solo las diferencias: borrar y volver a crear la misma etiqueta
        // en un mismo guardado choca con la clave (TransactionId, TagId).
        var wanted = list.Select(tag => tag.Id).ToHashSet();
        _tags.RemoveAll(tag => !wanted.Contains(tag.TagId));
        foreach (var tagId in wanted.Where(tagId => !_tags.Exists(tag => tag.TagId == tagId)))
        {
            _tags.Add(new TransactionTag(Id, tagId));
        }

        return Result.Success();
    }

    public Result ChangeCategory(Category category)
    {
        CategoryKind? expected = Kind switch
        {
            TransactionKind.Expense or TransactionKind.Refund => CategoryKind.Expense,
            TransactionKind.Income or TransactionKind.Yield => CategoryKind.Income,
            _ => null,
        };

        if (expected is null)
        {
            return TransactionErrors.CategoryNotAllowed;
        }

        if (category.LedgerId != LedgerId)
        {
            return TransactionErrors.CategoryFromAnotherLedger;
        }

        if (category.Kind != expected)
        {
            return TransactionErrors.CategoryKindMismatch;
        }

        CategoryId = category.Id;
        return Result.Success();
    }

    public Result ChangeDescription(string? description)
    {
        var error = ValidateDescription(description);
        if (error is not null)
        {
            return error;
        }

        Description = Text.Clean(description);
        return Result.Success();
    }

    /// <summary>Una pata de transferencia o de pago de resumen. La valida <see cref="TransferFactory"/>.</summary>
    internal static Transaction CreateLeg(
        Guid id,
        Account account,
        DateOnly date,
        Money signedAmount,
        TransactionKind kind,
        TransactionStatus status,
        Guid transferGroupId,
        Guid? paidStatementId,
        string? description) =>
        new(id, account, date, signedAmount, kind, status, categoryId: null, description)
        {
            TransferGroupId = transferGroupId,
            PaidStatementId = paidStatementId,
        };

    internal static Error? ValidateDescription(string? description) =>
        Text.Clean(description) is { Length: > DescriptionMaxLength } ? TransactionErrors.DescriptionTooLong : null;

    internal void ConfirmLeg(decimal? signedAmount, DateOnly? date)
    {
        if (signedAmount is decimal amount)
        {
            Amount = amount;
        }

        if (date is DateOnly newDate)
        {
            Date = newDate;
        }

        Status = TransactionStatus.Confirmed;
    }

    internal void MarkGeneratedBy(RecurringTemplate template, Period period)
    {
        RecurringTemplateId = template.Id;
        RecurringPeriod = period;
    }

    private void AddInstallments(InstallmentPlan? plan)
    {
        if (plan is null)
        {
            return;
        }

        foreach (var planned in plan.Installments)
        {
            _installments.Add(new Installment(
                SequentialGuid.New(), Id, planned.Statement.Id, planned.Number, planned.Total, planned.Amount));
        }
    }

    private static Error? ValidatePositive(Money amount) =>
        amount.IsPositive ? null : TransactionErrors.AmountMustBePositive;

    private static Error? ValidateAccount(Account account) =>
        account.IsArchived ? TransactionErrors.ArchivedAccount : null;

    private static Error? ValidateCategory(Account account, Category category, CategoryKind expected)
    {
        if (category.LedgerId != account.LedgerId)
        {
            return TransactionErrors.CategoryFromAnotherLedger;
        }

        return category.Kind == expected ? null : TransactionErrors.CategoryKindMismatch;
    }

    private static Error? ValidatePlan(Account account, Money charge, InstallmentPlan? plan)
    {
        if (!account.IsCredit)
        {
            return plan is null ? null : TransactionErrors.PlanOnlyForCredit;
        }

        if (plan is null)
        {
            return TransactionErrors.PlanRequiredForCredit;
        }

        if (plan.CreditAccountId != account.Id)
        {
            return TransactionErrors.PlanForAnotherAccount;
        }

        return plan.Installments.Count > 0 && plan.Total == charge ? null : TransactionErrors.PlanTotalMismatch;
    }
}
