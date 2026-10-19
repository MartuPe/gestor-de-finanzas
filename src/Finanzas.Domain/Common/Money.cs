using System.Globalization;

namespace Finanzas.Domain.Common;

/// <summary>Un monto en una moneda. No deja mezclar monedas ni usar más de 2 decimales.</summary>
public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        CurrencyCodes.EnsureValid(currency);
        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException($"El monto {amount} tiene más de 2 decimales.", nameof(amount));
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public bool IsNegative => Amount < 0m;

    public static Money Zero(string currency) => new(0m, currency);

    public static Money Ars(decimal amount) => new(amount, CurrencyCodes.Ars);

    public static Money Usd(decimal amount) => new(amount, CurrencyCodes.Usd);

    public Money Negate() => new(-Amount, Currency);

    public Money Abs() => new(Math.Abs(Amount), Currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money value) => value.Negate();

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Currency} {Amount:0.00}");

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"No se pueden operar montos en {left.Currency} y en {right.Currency}.");
        }
    }
}
