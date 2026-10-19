using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Statements;

public class StatementTests
{
    [Fact]
    public void Se_considera_cerrado_desde_el_dia_siguiente_al_cierre()
    {
        var octubre = Statement.Create(Guid.NewGuid(), Visa(), Mes(2025, 10), Fecha(2025, 9, 28), Fecha(2025, 10, 5)).Value;

        Assert.Equal(StatementEffectiveStatus.Open, octubre.EffectiveStatusOn(Fecha(2025, 9, 28)));
        Assert.Equal(StatementEffectiveStatus.Closed, octubre.EffectiveStatusOn(Fecha(2025, 9, 29)));
    }

    [Fact]
    public void El_vencimiento_tiene_que_caer_en_el_mes_del_resumen()
    {
        var result = Statement.Create(Guid.NewGuid(), Visa(), Mes(2025, 10), Fecha(2025, 9, 28), Fecha(2025, 11, 5));

        Assert.Equal(StatementErrors.DueOutsidePeriod, result.Error);
    }

    [Fact]
    public void El_resumen_de_una_tarjeta_necesita_fecha_de_cierre()
    {
        var result = Statement.Create(Guid.NewGuid(), Visa(), Mes(2025, 10), null, Fecha(2025, 10, 5));

        Assert.Equal(StatementErrors.ClosingRequired, result.Error);
    }

    [Fact]
    public void Un_resumen_sin_cargos_no_figura_como_pagado()
    {
        var octubre = Statement.Create(Guid.NewGuid(), Visa(), Mes(2025, 10), Fecha(2025, 9, 28), Fecha(2025, 10, 5)).Value;

        octubre.Settle(MoneyBag.Empty, MoneyBag.Empty);

        Assert.Equal(StatementStatus.Open, octubre.Status);
    }
}
