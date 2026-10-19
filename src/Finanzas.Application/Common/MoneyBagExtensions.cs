using Finanzas.Domain.Common;

namespace Finanzas.Application.Common;

public static class MoneyBagExtensions
{
    /// <summary>Formato de las respuestas: <c>{ "ARS": 50000.00, "USD": 40.00 }</c>.</summary>
    public static IReadOnlyDictionary<string, decimal> ToAmounts(this MoneyBag bag) =>
        bag.ToDictionary(money => money.Currency, money => money.Amount, StringComparer.Ordinal);

    public static MoneyBag Sum(this IEnumerable<MoneyBag> bags) =>
        bags.Aggregate(MoneyBag.Empty, (total, bag) => total + bag);

    /// <summary>
    /// Todos los montos pasados a moneda base con <paramref name="toBase"/>, o null si falta alguna cotización.
    /// A diferencia del disponible (R8b), acá se convierten también los montos positivos: es para totales y gráficos.
    /// </summary>
    public static decimal? ToBase(this MoneyBag amounts, string baseCurrency, IReadOnlyDictionary<string, decimal> toBase)
    {
        decimal total = 0;
        foreach (var money in amounts)
        {
            if (money.Currency == baseCurrency)
            {
                total += money.Amount;
            }
            else if (toBase.TryGetValue(money.Currency, out var rate))
            {
                total += decimal.Round(money.Amount * rate, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                return null;
            }
        }

        return total;
    }
}
