using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;

namespace Finanzas.Domain.Tests.Ledgers;

public class LedgerTests
{
    [Fact]
    public void Hoy_se_calcula_en_hora_argentina_y_no_en_UTC()
    {
        var ledger = Ledger.Create(Guid.NewGuid(), "Mis finanzas", Guid.NewGuid()).Value;

        // 31/10 a las 23:30 en Buenos Aires: en UTC ya es 1/11.
        var now = new DateTimeOffset(2026, 11, 1, 2, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 10, 31), ledger.TodayAt(now));
        Assert.Equal(new Period(2026, 10), ledger.PeriodAt(now));
    }

    [Fact]
    public void Arranca_con_el_blue_y_quien_lo_crea_queda_como_owner()
    {
        var ownerId = Guid.NewGuid();

        var ledger = Ledger.Create(Guid.NewGuid(), "Mis finanzas", ownerId).Value;

        Assert.Equal(ExchangeRateSources.Blue, ledger.ReferenceRateSource);
        Assert.Equal(CurrencyCodes.Ars, ledger.BaseCurrency);
        var owner = Assert.Single(ledger.Members);
        Assert.Equal((ownerId, LedgerRole.Owner), (owner.UserId, owner.Role));
    }

    [Fact]
    public void Rechaza_una_casa_de_cotizacion_que_no_existe()
    {
        var result = Ledger.Create(Guid.NewGuid(), "Mis finanzas", Guid.NewGuid(), referenceRateSource: "paralelo");

        Assert.Equal(LedgerErrors.InvalidRateSource, result.Error);
    }

    [Fact]
    public void Tiene_un_solo_owner()
    {
        var ledger = Ledger.Create(Guid.NewGuid(), "Mis finanzas", Guid.NewGuid()).Value;

        Assert.Equal(LedgerErrors.SingleOwner, ledger.AddMember(Guid.NewGuid(), LedgerRole.Owner).Error);
        Assert.True(ledger.AddMember(Guid.NewGuid(), LedgerRole.Editor).IsSuccess);
    }
}
