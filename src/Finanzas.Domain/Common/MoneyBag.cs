using System.Collections;

namespace Finanzas.Domain.Common;

/// <summary>Montos agrupados por moneda: el total de un resumen, un saldo, el disponible.</summary>
public sealed class MoneyBag : IEnumerable<Money>
{
    private readonly SortedDictionary<string, decimal> _amounts;

    private MoneyBag(SortedDictionary<string, decimal> amounts) => _amounts = amounts;

    public static MoneyBag Empty { get; } = new(new SortedDictionary<string, decimal>(StringComparer.Ordinal));

    /// <summary>Monto en esa moneda; 0 si no hay ninguno.</summary>
    public decimal this[string currency] => _amounts.GetValueOrDefault(currency);

    public IReadOnlyCollection<string> Currencies => _amounts.Keys;

    public bool IsZero => _amounts.Values.All(amount => amount == 0m);

    public static MoneyBag Of(params IEnumerable<Money> items)
    {
        var amounts = new SortedDictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            amounts[item.Currency] = amounts.GetValueOrDefault(item.Currency) + item.Amount;
        }

        return new MoneyBag(amounts);
    }

    public MoneyBag Plus(MoneyBag other) => Of(this.Concat(other));

    public MoneyBag Minus(MoneyBag other) => Of(this.Concat(other.Select(money => money.Negate())));

    public static MoneyBag operator +(MoneyBag left, MoneyBag right) => left.Plus(right);

    public static MoneyBag operator -(MoneyBag left, MoneyBag right) => left.Minus(right);

    public IEnumerator<Money> GetEnumerator() =>
        _amounts.Select(pair => new Money(pair.Value, pair.Key)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => _amounts.Count == 0 ? "(vacío)" : string.Join(" + ", this);
}
