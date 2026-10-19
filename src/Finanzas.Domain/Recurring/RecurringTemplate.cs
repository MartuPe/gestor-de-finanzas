using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;

namespace Finanzas.Domain.Recurring;

/// <summary>
/// Un gasto, ingreso o transferencia que se repite y se genera solo como previsto:
/// el sueldo, el aguinaldo cada 6 meses, el ahorro mensual, YouTube Premium en la Visa.
/// </summary>
public sealed class RecurringTemplate : Entity, IAggregateRoot
{
    public const int DescriptionMaxLength = 200;
    public const int MaxInterval = 24;

    private RecurringTemplate()
    {
    }

    private RecurringTemplate(
        Guid id,
        Guid ledgerId,
        string description,
        RecurringKind kind,
        Guid accountId,
        Guid? toAccountId,
        Guid? categoryId,
        Money estimatedAmount,
        RecurringFrequency frequency,
        int interval,
        int dayOfMonth,
        DateOnly startsOn,
        DateOnly? endsOn)
        : base(id)
    {
        LedgerId = ledgerId;
        Description = description;
        Kind = kind;
        AccountId = accountId;
        ToAccountId = toAccountId;
        CategoryId = categoryId;
        EstimatedAmount = estimatedAmount.Amount;
        Currency = estimatedAmount.Currency;
        Frequency = frequency;
        Interval = interval;
        DayOfMonth = dayOfMonth;
        StartsOn = startsOn;
        EndsOn = endsOn;
        IsActive = true;
    }

    public Guid LedgerId { get; private set; }

    public string Description { get; private set; } = null!;

    public RecurringKind Kind { get; private set; }

    /// <summary>Cuenta del gasto o del ingreso, u origen de la transferencia.</summary>
    public Guid AccountId { get; private set; }

    public Guid? ToAccountId { get; private set; }

    public Guid? CategoryId { get; private set; }

    /// <summary>En valor absoluto: el signo lo da <see cref="Kind"/>.</summary>
    public decimal EstimatedAmount { get; private set; }

    public string Currency { get; private set; } = null!;

    public RecurringFrequency Frequency { get; private set; }

    /// <summary>Cada cuántos períodos se repite: el aguinaldo es mensual con intervalo 6.</summary>
    public int Interval { get; private set; }

    public int DayOfMonth { get; private set; }

    public DateOnly StartsOn { get; private set; }

    public DateOnly? EndsOn { get; private set; }

    public bool IsActive { get; private set; }

    public Money EstimatedMoney => new(EstimatedAmount, Currency);

    public static Result<RecurringTemplate> Create(
        Guid id,
        string description,
        RecurringKind kind,
        Account account,
        Account? toAccount,
        Category? category,
        Money estimatedAmount,
        RecurringFrequency frequency,
        int interval,
        int dayOfMonth,
        DateOnly startsOn,
        DateOnly? endsOn = null)
    {
        var cleanDescription = Text.Clean(description);
        if (cleanDescription is null || cleanDescription.Length > DescriptionMaxLength)
        {
            return RecurringErrors.InvalidDescription;
        }

        var error = ValidateSchedule(estimatedAmount, interval, dayOfMonth, startsOn, endsOn)
            ?? ValidateAccounts(kind, account, toAccount)
            ?? ValidateCategory(kind, account, category);
        if (error is not null)
        {
            return error;
        }

        return new RecurringTemplate(
            id,
            account.LedgerId,
            cleanDescription,
            kind,
            account.Id,
            toAccount?.Id,
            kind == RecurringKind.Transfer ? null : category!.Id,
            estimatedAmount,
            frequency,
            interval,
            dayOfMonth,
            startsOn,
            endsOn);
    }

    /// <summary>Si la plantilla genera un movimiento en <paramref name="period"/>.</summary>
    public bool OccursIn(Period period)
    {
        var start = Period.FromDate(StartsOn);
        if (!IsActive || period < start)
        {
            return false;
        }

        var step = Frequency == RecurringFrequency.Monthly ? Interval : 12 * Interval;
        if (start.MonthsUntil(period) % step != 0)
        {
            return false;
        }

        var date = OccurrenceDate(period);
        return date >= StartsOn && (EndsOn is null || date <= EndsOn);
    }

    /// <summary>Fecha del movimiento en ese mes. El día 31 en febrero es el último día de febrero.</summary>
    public DateOnly OccurrenceDate(Period period) => period.Day(DayOfMonth);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static Error? ValidateSchedule(Money amount, int interval, int dayOfMonth, DateOnly startsOn, DateOnly? endsOn)
    {
        if (!amount.IsPositive)
        {
            return RecurringErrors.AmountMustBePositive;
        }

        if (interval is < 1 or > MaxInterval)
        {
            return RecurringErrors.InvalidInterval;
        }

        if (dayOfMonth is < 1 or > 31)
        {
            return RecurringErrors.InvalidDay;
        }

        return endsOn < startsOn ? RecurringErrors.EndsBeforeStart : null;
    }

    private static Error? ValidateAccounts(RecurringKind kind, Account account, Account? toAccount)
    {
        if (account.IsArchived || toAccount?.IsArchived == true)
        {
            return RecurringErrors.ArchivedAccount;
        }

        if (kind != RecurringKind.Transfer)
        {
            if (toAccount is not null)
            {
                return RecurringErrors.DestinationOnlyForTransfer;
            }

            return kind == RecurringKind.Income && account.IsCredit ? RecurringErrors.IncomeOnCredit : null;
        }

        if (toAccount is null)
        {
            return RecurringErrors.TransferNeedsDestination;
        }

        if (toAccount.Id == account.Id || toAccount.LedgerId != account.LedgerId)
        {
            return RecurringErrors.InvalidDestination;
        }

        return account.IsCredit || toAccount.IsCredit ? RecurringErrors.TransferWithCredit : null;
    }

    private static Error? ValidateCategory(RecurringKind kind, Account account, Category? category)
    {
        if (kind == RecurringKind.Transfer)
        {
            return null;
        }

        if (category is null)
        {
            return RecurringErrors.CategoryRequired;
        }

        if (category.LedgerId != account.LedgerId)
        {
            return RecurringErrors.CategoryFromAnotherLedger;
        }

        var expected = kind == RecurringKind.Expense ? CategoryKind.Expense : CategoryKind.Income;
        return category.Kind == expected ? null : RecurringErrors.CategoryKindMismatch;
    }
}
