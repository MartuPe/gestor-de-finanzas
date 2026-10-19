using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Accounts;

public class AccountTests
{
    [Fact]
    public void Solo_banco_efectivo_y_billeteras_son_liquidas()
    {
        Assert.True(Banco().IsLiquid);
        Assert.True(MercadoPago().IsLiquid);
        Assert.False(Ahorro().IsLiquid);
        Assert.False(Visa().IsLiquid);
    }

    [Fact]
    public void Una_tarjeta_necesita_dia_de_cierre()
    {
        var result = Account.CreateCredit(
            Guid.NewGuid(), LedgerId, "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", null, 5);

        Assert.Equal(AccountErrors.CardNeedsClosingDay, result.Error);
    }

    [Fact]
    public void Las_tarjetas_se_crean_con_los_datos_de_la_tarjeta()
    {
        var result = Account.Create(Guid.NewGuid(), LedgerId, "Visa", AccountKind.Credit, CurrencyCodes.Ars);

        Assert.Equal(AccountErrors.UseCreateCredit, result.Error);
    }
}

public class CreditLineTests
{
    [Fact]
    public void Visa_que_cierra_el_28_y_vence_el_5()
    {
        var (closing, due) = Visa().CreditLine!.EstimateDates(Mes(2025, 10));

        Assert.Equal(Fecha(2025, 9, 28), closing);
        Assert.Equal(Fecha(2025, 10, 5), due);
    }

    [Fact]
    public void Una_tarjeta_que_cierra_y_vence_en_el_mismo_mes()
    {
        var card = Account.CreateCredit(
            Guid.NewGuid(), LedgerId, "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", 2, 12).Value;

        var (closing, due) = card.CreditLine!.EstimateDates(Mes(2026, 2));

        Assert.Equal(Fecha(2026, 2, 2), closing);
        Assert.Equal(Fecha(2026, 2, 12), due);
    }

    [Fact]
    public void Con_vencimiento_el_31_en_febrero_el_cierre_pasa_al_mes_anterior()
    {
        var card = Account.CreateCredit(
            Guid.NewGuid(), LedgerId, "Tarjeta", CurrencyCodes.Ars, CreditLineType.Card, "Banco", "visa", 30, 31).Value;

        var (closing, due) = card.CreditLine!.EstimateDates(Mes(2026, 2));

        Assert.Equal(Fecha(2026, 1, 30), closing);
        Assert.Equal(Fecha(2026, 2, 28), due);
    }

    [Fact]
    public void Mercado_Credito_no_tiene_cierre()
    {
        var (closing, due) = MercadoCredito().CreditLine!.EstimateDates(Mes(2026, 6));

        Assert.Null(closing);
        Assert.Equal(Fecha(2026, 6, 10), due);
    }
}
