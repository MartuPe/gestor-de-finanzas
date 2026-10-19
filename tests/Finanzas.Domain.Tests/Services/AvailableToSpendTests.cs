using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Services;

public class AvailableToSpendTests
{
    private static readonly Dictionary<string, decimal> Blue = new() { [CurrencyCodes.Usd] = 1_555m };

    [Fact]
    public void Octubre_2026_con_el_blue_del_dia()
    {
        var disponible = Calculate(Octubre2026(), Blue);

        Assert.Equal(460_000m, disponible.ByCurrency[CurrencyCodes.Ars]);
        Assert.Equal(-40m, disponible.ByCurrency[CurrencyCodes.Usd]);
        Assert.Equal(397_800m, disponible.Unified); // 460.000 − 40 × 1.555
    }

    [Fact]
    public void Octubre_2026_a_1500_da_lo_mismo_que_la_celda_K10_del_Excel()
    {
        var disponible = Calculate(Octubre2026(), new Dictionary<string, decimal> { [CurrencyCodes.Usd] = 1_500m });

        Assert.Equal(400_000m, disponible.Unified);
    }

    [Fact]
    public void Septiembre_2025_cierra_en_cero_como_en_el_Excel()
    {
        // Celda K8: saldo total − (gastos fijos + variables sin tarjeta + ahorro) = 0.
        var septiembre = new MonthPosition(
            Mes(2025, 9),
            LiquidAtStart: MoneyBag.Of(Money.Ars(600_000)),
            ConfirmedFlows: MoneyBag.Of(Money.Ars(-313_700)),
            PlannedPending: MoneyBag.Of(Money.Ars(-100_000)),
            StatementsOutstanding: MoneyBag.Of(Money.Ars(143_300), Money.Ars(43_000))); // Visa + Mastercard

        Assert.Equal(0m, Calculate(septiembre, Blue).Unified);
    }

    [Fact]
    public void Si_tenes_dolares_para_cubrir_Claude_no_te_resta_pesos()
    {
        // Mayo de 2026: "este mes de claude está cubierto por dls que compré el mes pasado".
        var position = Octubre2026() with { LiquidAtStart = MoneyBag.Of(Money.Ars(1_800_000), Money.Usd(40)) };

        var disponible = Calculate(position, Blue);

        Assert.Equal(0m, disponible.ByCurrency[CurrencyCodes.Usd]);
        Assert.Equal(460_000m, disponible.Unified);
    }

    [Fact]
    public void Los_dolares_que_sobran_no_se_suman_a_los_pesos()
    {
        var position = Octubre2026() with { LiquidAtStart = MoneyBag.Of(Money.Ars(1_800_000), Money.Usd(500)) };

        Assert.Equal(460_000m, Calculate(position, Blue).Unified);
    }

    [Fact]
    public void Sin_cotizacion_no_se_puede_unificar()
    {
        var disponible = Calculate(Octubre2026(), new Dictionary<string, decimal>());

        Assert.Null(disponible.Unified);
        Assert.Equal(new[] { CurrencyCodes.Usd }, disponible.MissingRates);
    }

    /// <summary>La hoja Octubre2026 del Excel.</summary>
    private static MonthPosition Octubre2026() => new(
        Mes(2026, 10),
        LiquidAtStart: MoneyBag.Of(Money.Ars(1_800_000)),
        ConfirmedFlows: MoneyBag.Of(Money.Ars(-406_000)), // gastos variables
        PlannedPending: MoneyBag.Of(Money.Ars(-800_000)), // ahorro mensual
        StatementsOutstanding: MoneyBag.Of(
            Money.Ars(75_000), // Mastercard
            Money.Ars(50_000), // Patagonia PES
            Money.Ars(9_000), // Mercado Crédito
            Money.Usd(40))); // Patagonia USD: Claude

    private static AvailableToSpend Calculate(MonthPosition position, IReadOnlyDictionary<string, decimal> rates) =>
        AvailableToSpendCalculator.Calculate(position, CurrencyCodes.Ars, rates);
}
