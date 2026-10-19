using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;

namespace Finanzas.Domain.Transactions;

/// <summary>Las dos patas de una transferencia o de un pago de resumen. Se guardan juntas.</summary>
public sealed record TransferLegs(Transaction Out, Transaction In)
{
    /// <summary>
    /// R2: la cotización real de la operación, en <paramref name="baseCurrency"/> por unidad de la otra moneda.
    /// Comprar USD 100 con ARS 155.500 da 1.555. Es null si las dos patas están en la misma moneda.
    /// </summary>
    public decimal? ImpliedRate(string baseCurrency)
    {
        if (Out.Currency == In.Currency)
        {
            return null;
        }

        var (baseLeg, otherLeg) = Out.Currency == baseCurrency ? (Out, In) : (In, Out);
        return Math.Abs(baseLeg.Amount) / Math.Abs(otherLeg.Amount);
    }
}

/// <summary>Crea transferencias y pagos de resumen garantizando que las dos patas sean coherentes (R2, R6).</summary>
public static class TransferFactory
{
    /// <summary>
    /// Mueve plata entre dos cuentas de dinero. Si las monedas difieren, es una compra o una venta de dólares.
    /// Los montos van en positivo.
    /// </summary>
    public static Result<TransferLegs> CreateTransfer(
        Guid outId,
        Guid inId,
        Account from,
        Account to,
        DateOnly date,
        Money sent,
        Money received,
        string? description = null,
        TransactionStatus status = TransactionStatus.Confirmed)
    {
        var error = ValidateLegs(from, to, sent, received, description)
            ?? (from.IsCredit || to.IsCredit ? TransferErrors.CreditAccountsUseCardPayment : null);
        if (error is not null)
        {
            return error;
        }

        var group = SequentialGuid.New();
        return new TransferLegs(
            Transaction.CreateLeg(outId, from, date, sent.Negate(), TransactionKind.Transfer, status, group, null, description),
            Transaction.CreateLeg(inId, to, date, received, TransactionKind.Transfer, status, group, null, description));
    }

    /// <summary>
    /// R6: paga un resumen. La pata de la cuenta de crédito va en la moneda del resumen;
    /// la de origen puede estar en otra (por ejemplo, la parte en dólares pagada con pesos).
    /// </summary>
    public static Result<TransferLegs> CreateCardPayment(
        Guid outId,
        Guid inId,
        Account from,
        Account creditAccount,
        Statement statement,
        DateOnly date,
        Money paid,
        Money credited,
        string? description = null)
    {
        var error = (creditAccount.IsCredit ? null : TransferErrors.NotCreditAccount)
            ?? (from.IsCredit ? TransferErrors.PayFromCreditAccount : null)
            ?? (statement.CreditAccountId == creditAccount.Id ? null : TransferErrors.StatementOfAnotherAccount)
            ?? ValidateLegs(from, creditAccount, paid, credited, description);
        if (error is not null)
        {
            return error;
        }

        var group = SequentialGuid.New();
        return new TransferLegs(
            Transaction.CreateLeg(
                outId, from, date, paid.Negate(), TransactionKind.CardPayment, TransactionStatus.Confirmed, group, statement.Id, description),
            Transaction.CreateLeg(
                inId, creditAccount, date, credited, TransactionKind.CardPayment, TransactionStatus.Confirmed, group, statement.Id, description));
    }

    /// <summary>R7 para transferencias previstas, como el ahorro mensual: las dos patas se confirman juntas.</summary>
    public static Result Confirm(TransferLegs legs, DateOnly? actualDate = null, Money? sent = null, Money? received = null)
    {
        if (legs.Out.TransferGroupId is null || legs.Out.TransferGroupId != legs.In.TransferGroupId)
        {
            return TransferErrors.NotSameTransfer;
        }

        if (!legs.Out.IsPlanned || !legs.In.IsPlanned)
        {
            return TransactionErrors.AlreadyConfirmed;
        }

        var newSent = sent ?? legs.Out.Money.Negate();
        var newReceived = received ?? legs.In.Money;
        if (newSent.Currency != legs.Out.Currency || newReceived.Currency != legs.In.Currency)
        {
            return TransactionErrors.CurrencyMismatch;
        }

        if (!newSent.IsPositive || !newReceived.IsPositive)
        {
            return TransferErrors.AmountMustBePositive;
        }

        if (newSent.Currency == newReceived.Currency && newSent != newReceived)
        {
            return TransferErrors.SameCurrencyDifferentAmounts;
        }

        legs.Out.ConfirmLeg(-newSent.Amount, actualDate);
        legs.In.ConfirmLeg(newReceived.Amount, actualDate);
        return Result.Success();
    }

    private static Error? ValidateLegs(Account from, Account to, Money sent, Money received, string? description)
    {
        if (from.Id == to.Id)
        {
            return TransferErrors.SameAccount;
        }

        if (from.LedgerId != to.LedgerId)
        {
            return TransferErrors.DifferentLedger;
        }

        if (from.IsArchived || to.IsArchived)
        {
            return TransactionErrors.ArchivedAccount;
        }

        if (!sent.IsPositive || !received.IsPositive)
        {
            return TransferErrors.AmountMustBePositive;
        }

        if (sent.Currency == received.Currency && sent != received)
        {
            return TransferErrors.SameCurrencyDifferentAmounts;
        }

        return Transaction.ValidateDescription(description);
    }
}
