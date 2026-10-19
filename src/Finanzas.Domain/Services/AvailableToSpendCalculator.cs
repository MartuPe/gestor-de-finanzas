using Finanzas.Domain.Common;

namespace Finanzas.Domain.Services;

/// <summary>
/// Lo que el cálculo necesita saber de un mes. Todo es por moneda y solo de cuentas líquidas.
/// Arma estos datos la consulta de lectura de la capa Application.
/// </summary>
/// <param name="LiquidAtStart">Saldo confirmado de las cuentas líquidas al empezar el mes.</param>
/// <param name="ConfirmedFlows">Suma con signo de los movimientos confirmados del mes en cuentas líquidas.</param>
/// <param name="PlannedPending">Suma con signo de los previstos hasta fin de mes, incluidos los vencidos sin confirmar.</param>
/// <param name="StatementsOutstanding">Lo que falta pagar de los resúmenes que vencen hasta fin de mes.</param>
public sealed record MonthPosition(
    Period Period,
    MoneyBag LiquidAtStart,
    MoneyBag ConfirmedFlows,
    MoneyBag PlannedPending,
    MoneyBag StatementsOutstanding);

/// <param name="ByCurrency">Disponible en cada moneda. Puede ser negativo: USD -40 quiere decir que faltan dólares.</param>
/// <param name="Unified">Disponible en moneda base, o null si falta alguna cotización (ver <paramref name="MissingRates"/>).</param>
public sealed record AvailableToSpend(
    Period Period,
    MoneyBag ByCurrency,
    string BaseCurrency,
    decimal? Unified,
    IReadOnlyList<string> MissingRates);

/// <summary>El "Disponible para gastar" de tu Excel (R8, R8b).</summary>
public static class AvailableToSpendCalculator
{
    /// <param name="ratesToBase">Cuántas unidades de moneda base vale 1 unidad de cada moneda (el blue de venta para USD).</param>
    public static AvailableToSpend Calculate(
        MonthPosition position,
        string baseCurrency,
        IReadOnlyDictionary<string, decimal> ratesToBase)
    {
        // R8: saldo al inicio + movimientos del mes + previstos − resúmenes por pagar.
        var byCurrency = position.LiquidAtStart
            + position.ConfirmedFlows
            + position.PlannedPending
            - position.StatementsOutstanding;

        // R8b: los dólares que sobran no se suman a los pesos, pero los que faltan se compran al blue y restan.
        var unified = byCurrency[baseCurrency];
        var missing = new List<string>();
        foreach (var shortfall in byCurrency.Where(money => money.Currency != baseCurrency && money.IsNegative))
        {
            if (ratesToBase.TryGetValue(shortfall.Currency, out var rate))
            {
                unified += decimal.Round(shortfall.Amount * rate, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                missing.Add(shortfall.Currency);
            }
        }

        return new AvailableToSpend(position.Period, byCurrency, baseCurrency, missing.Count == 0 ? unified : null, missing);
    }
}
