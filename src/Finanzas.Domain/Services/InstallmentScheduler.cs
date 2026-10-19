using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;

namespace Finanzas.Domain.Services;

/// <summary>Reparte las compras a crédito en cuotas y decide en qué resumen cae cada una (R3, R4, R5).</summary>
public static class InstallmentScheduler
{
    public const int MaxInstallments = 60;

    /// <summary>R3: partes iguales, truncadas a 2 decimales. El redondeo va a la última cuota.</summary>
    public static IReadOnlyList<Money> Split(Money total, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var part = decimal.Round(total.Amount / count, 2, MidpointRounding.ToZero);
        var parts = Enumerable.Repeat(new Money(part, total.Currency), count - 1).ToList();
        parts.Add(new Money(total.Amount - (part * (count - 1)), total.Currency));
        return parts;
    }

    /// <summary>
    /// R4: la cuota 1 va al primer resumen cuyo corte es igual o posterior a la compra,
    /// y la cuota k va k−1 resúmenes después. Los resúmenes que falten se estiman.
    /// </summary>
    /// <param name="amount">Lo que se carga en la línea: positivo en una compra, negativo en una devolución (en 1 pago).</param>
    /// <param name="firstNumber">Para importar compras que ya venían en curso: "Perfume Lu (2/3)" arranca en la cuota 2.</param>
    public static Result<InstallmentPlan> Plan(
        Account creditAccount,
        DateOnly purchaseDate,
        Money amount,
        int installments,
        IEnumerable<Statement> existingStatements,
        int firstNumber = 1)
    {
        if (!creditAccount.IsCredit)
        {
            return InstallmentErrors.NotCreditAccount;
        }

        return Plan(new StatementBook(creditAccount, existingStatements), creditAccount, purchaseDate, amount, installments, firstNumber);
    }

    /// <summary>
    /// R5: después de cambiar las fechas de un resumen, vuelve a ubicar las cuotas de una compra.
    /// No mueve nada si alguna cuota sale de un resumen con pagos o entra en uno.
    /// </summary>
    /// <returns>Los resúmenes estimados que hubo que crear.</returns>
    public static Result<IReadOnlyList<Statement>> Reschedule(
        Transaction purchase,
        Account creditAccount,
        IEnumerable<Statement> statements,
        IReadOnlySet<Guid> statementsWithPayments)
    {
        if (!purchase.IsCreditPurchase || purchase.AccountId != creditAccount.Id || !creditAccount.IsCredit)
        {
            return InstallmentErrors.NotCreditPurchaseOfAccount;
        }

        var book = new StatementBook(creditAccount, statements);
        var first = book.FirstAccepting(purchase.Date);
        var installments = purchase.Installments.OrderBy(installment => installment.Number).ToList();
        var firstNumber = installments[0].Number;

        var moves = new List<(Installment Installment, Statement Target)>();
        foreach (var installment in installments)
        {
            var target = book.For(first.Period.AddMonths(installment.Number - firstNumber));
            if (target.Id == installment.StatementId)
            {
                continue;
            }

            if (statementsWithPayments.Contains(installment.StatementId) || statementsWithPayments.Contains(target.Id))
            {
                return StatementErrors.HasPayments;
            }

            moves.Add((installment, target));
        }

        foreach (var (installment, target) in moves)
        {
            installment.MoveTo(target.Id);
        }

        return Result<IReadOnlyList<Statement>>.Success(book.Created);
    }

    internal static Result<InstallmentPlan> Plan(
        StatementBook book,
        Account creditAccount,
        DateOnly purchaseDate,
        Money amount,
        int installments,
        int firstNumber = 1)
    {
        if (installments is < 1 or > MaxInstallments)
        {
            return InstallmentErrors.InvalidCount;
        }

        if (firstNumber < 1 || firstNumber > installments)
        {
            return InstallmentErrors.InvalidFirstNumber;
        }

        if (amount.IsZero)
        {
            return InstallmentErrors.AmountRequired;
        }

        if (amount.IsNegative && installments != 1)
        {
            return InstallmentErrors.RefundInOnePayment;
        }

        var createdBefore = book.Created.Count;
        var first = book.FirstAccepting(purchaseDate);
        var amounts = Split(amount, installments - firstNumber + 1);

        var planned = amounts
            .Select((part, index) => new PlannedInstallment(
                firstNumber + index,
                installments,
                part,
                book.For(first.Period.AddMonths(index))))
            .ToList();

        var newStatements = book.Created.Skip(createdBefore).ToList();
        return new InstallmentPlan(creditAccount.Id, planned, newStatements);
    }
}
