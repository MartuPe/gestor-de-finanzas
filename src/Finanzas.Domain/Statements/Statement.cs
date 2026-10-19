using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;

namespace Finanzas.Domain.Statements;

/// <summary>
/// Resumen de una línea de crédito para un mes de vencimiento. Su total no se guarda:
/// es la suma de las cuotas que caen en él, por moneda.
/// </summary>
public sealed class Statement : Entity, IAggregateRoot
{
    public const int NoteMaxLength = 500;

    private Statement()
    {
    }

    private Statement(
        Guid id,
        Guid creditAccountId,
        Period period,
        DateOnly? closingDate,
        DateOnly dueDate,
        bool isEstimated)
        : base(id)
    {
        CreditAccountId = creditAccountId;
        Period = period;
        ClosingDate = closingDate;
        DueDate = dueDate;
        IsEstimated = isEstimated;
        Status = StatementStatus.Open;
    }

    public Guid CreditAccountId { get; private set; }

    /// <summary>Mes de vencimiento: en el disponible de ese mes se resta este resumen.</summary>
    public Period Period { get; private set; }

    public DateOnly? ClosingDate { get; private set; }

    public DateOnly DueDate { get; private set; }

    public StatementStatus Status { get; private set; }

    /// <summary>Las fechas se propusieron a partir de los días habituales; todavía no llegó el resumen real.</summary>
    public bool IsEstimated { get; private set; }

    public string? Note { get; private set; }

    /// <summary>Última fecha de compra que entra en este resumen (R4).</summary>
    public DateOnly Cutoff => ClosingDate ?? DueDate.AddDays(-1);

    /// <summary>Resumen con fechas reales.</summary>
    public static Result<Statement> Create(
        Guid id,
        Account creditAccount,
        Period period,
        DateOnly? closingDate,
        DateOnly dueDate)
    {
        if (creditAccount.CreditLine is not CreditLine line)
        {
            return StatementErrors.NotCreditAccount;
        }

        var error = ValidateDates(line, period, closingDate, dueDate);
        if (error is not null)
        {
            return error;
        }

        return new Statement(id, creditAccount.Id, period, closingDate, dueDate, isEstimated: false);
    }

    public StatementEffectiveStatus EffectiveStatusOn(DateOnly today)
    {
        if (Status == StatementStatus.Paid)
        {
            return StatementEffectiveStatus.Paid;
        }

        return today > Cutoff ? StatementEffectiveStatus.Closed : StatementEffectiveStatus.Open;
    }

    /// <summary>
    /// Carga las fechas reales. Después hay que reasignar las cuotas de las compras afectadas
    /// con <c>InstallmentScheduler.Reschedule</c> (R5).
    /// </summary>
    public Result ChangeDates(Account creditAccount, DateOnly? closingDate, DateOnly dueDate, bool hasPayments)
    {
        if (creditAccount.Id != CreditAccountId || creditAccount.CreditLine is not CreditLine line)
        {
            return StatementErrors.NotCreditAccount;
        }

        if (hasPayments)
        {
            return StatementErrors.HasPayments;
        }

        var error = ValidateDates(line, Period, closingDate, dueDate);
        if (error is not null)
        {
            return error;
        }

        ClosingDate = closingDate;
        DueDate = dueDate;
        IsEstimated = false;
        return Result.Success();
    }

    public Result ChangeNote(string? note)
    {
        var clean = Text.Clean(note);
        if (clean is { Length: > NoteMaxLength })
        {
            return StatementErrors.NoteTooLong;
        }

        Note = clean;
        return Result.Success();
    }

    /// <summary>Lo cargado en este resumen, por moneda.</summary>
    public MoneyBag ChargesFrom(IEnumerable<Installment> installments) =>
        MoneyBag.Of(installments.Where(installment => installment.StatementId == Id).Select(installment => installment.Money));

    /// <summary>Lo pagado de este resumen, por moneda: la pata de cada pago que cae en la cuenta de crédito.</summary>
    public MoneyBag PaymentsFrom(IEnumerable<Transaction> transactions) =>
        MoneyBag.Of(transactions
            .Where(transaction => transaction.Kind == TransactionKind.CardPayment
                && transaction.PaidStatementId == Id
                && transaction.AccountId == CreditAccountId
                && transaction.Status == TransactionStatus.Confirmed)
            .Select(transaction => transaction.Money));

    /// <summary>Lo que falta pagar en cada moneda. Nunca es negativo.</summary>
    public static MoneyBag Outstanding(MoneyBag charges, MoneyBag payments) =>
        MoneyBag.Of(charges.Select(charge =>
            new Money(Math.Max(0m, charge.Amount - payments[charge.Currency]), charge.Currency)));

    /// <summary>R6: el resumen queda pagado cuando, en cada moneda, lo pagado cubre lo cargado.</summary>
    public void Settle(MoneyBag charges, MoneyBag payments)
    {
        var owesSomething = charges.Any(charge => charge.IsPositive);
        Status = owesSomething && Outstanding(charges, payments).IsZero
            ? StatementStatus.Paid
            : StatementStatus.Open;
    }

    /// <summary>Resumen futuro con fechas propuestas a partir de los días habituales de la línea.</summary>
    internal static Statement Estimate(Account creditAccount, Period period)
    {
        var line = creditAccount.CreditLine
            ?? throw new InvalidOperationException("Solo se estiman resúmenes de cuentas de crédito.");
        var (closing, due) = line.EstimateDates(period);
        return new Statement(SequentialGuid.New(), creditAccount.Id, period, closing, due, isEstimated: true);
    }

    private static Error? ValidateDates(CreditLine line, Period period, DateOnly? closingDate, DateOnly dueDate)
    {
        if (!period.Contains(dueDate))
        {
            return StatementErrors.DueOutsidePeriod;
        }

        if (line.LineType == CreditLineType.Card && closingDate is null)
        {
            return StatementErrors.ClosingRequired;
        }

        if (closingDate >= dueDate)
        {
            return StatementErrors.ClosingAfterDue;
        }

        return null;
    }
}
