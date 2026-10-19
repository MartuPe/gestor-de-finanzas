using System.Data.SqlTypes;
using Finanzas.Domain.Common;

namespace Finanzas.Domain.Tests.Common;

public class MoneyTests
{
    [Fact]
    public void No_se_pueden_sumar_pesos_con_dolares() =>
        Assert.Throws<InvalidOperationException>(() => Money.Ars(1_000) + Money.Usd(1));

    [Fact]
    public void Un_monto_no_admite_mas_de_dos_decimales() =>
        Assert.Throws<ArgumentException>(() => Money.Ars(10.005m));

    [Fact]
    public void MoneyBag_suma_por_moneda()
    {
        var bag = MoneyBag.Of(Money.Ars(50_000), Money.Usd(40), Money.Ars(25_000));

        Assert.Equal(75_000m, bag[CurrencyCodes.Ars]);
        Assert.Equal(40m, bag[CurrencyCodes.Usd]);
        Assert.Equal(0m, bag["EUR"]);
    }
}

public class PeriodTests
{
    [Fact]
    public void El_31_de_febrero_es_el_ultimo_dia_de_febrero()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), new Period(2026, 2).Day(31));
        Assert.Equal(new DateOnly(2028, 2, 29), new Period(2028, 2).Day(31));
    }

    [Fact]
    public void Despues_de_diciembre_viene_enero_del_anio_siguiente() =>
        Assert.Equal(new Period(2026, 1), new Period(2025, 12).Next());

    [Fact]
    public void Se_escribe_y_se_lee_como_yyyy_MM()
    {
        Assert.Equal("2026-10", new Period(2026, 10).ToString());
        Assert.Equal(new Period(2026, 10), Period.Parse("2026-10"));
        Assert.False(Period.TryParse("2026-13", out _));
        Assert.False(Period.TryParse("26-10", out _));
    }

    [Fact]
    public void Cuenta_los_meses_entre_dos_periodos() =>
        Assert.Equal(13, new Period(2025, 9).MonthsUntil(new Period(2026, 10)));
}

public class SequentialGuidTests
{
    [Fact]
    public void SQL_Server_los_ordena_en_el_orden_en_que_se_generaron()
    {
        var guids = Enumerable.Range(0, 1_000).Select(_ => SequentialGuid.New()).ToList();

        var sqlServerOrder = guids.OrderBy(guid => new SqlGuid(guid)).ToList();

        Assert.Equal(guids, sqlServerOrder);
    }
}
