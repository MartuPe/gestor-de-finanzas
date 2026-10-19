using System.Globalization;
using Finanzas.Domain.Common;

namespace Finanzas.Domain.Transactions;

/// <summary>
/// Una cuota de una compra a crédito, asignada al resumen en el que vence.
/// Positiva si es un cargo y negativa si es una devolución.
/// </summary>
public sealed class Installment : Entity
{
    private Installment()
    {
    }

    internal Installment(Guid id, Guid transactionId, Guid statementId, int number, int total, Money amount)
        : base(id)
    {
        TransactionId = transactionId;
        StatementId = statementId;
        Number = number;
        Total = total;
        Amount = amount.Amount;
        Currency = amount.Currency;
    }

    public Guid TransactionId { get; private set; }

    public Guid StatementId { get; private set; }

    public int Number { get; private set; }

    public int Total { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public Money Money => new(Amount, Currency);

    /// <summary>Por ejemplo "2/3".</summary>
    public string Label => string.Create(CultureInfo.InvariantCulture, $"{Number}/{Total}");

    internal void ChangeAmount(decimal amount) => Amount = amount;

    internal void MoveTo(Guid statementId) => StatementId = statementId;
}
