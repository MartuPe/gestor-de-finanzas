using Finanzas.Domain.Common;
using Finanzas.Domain.Tags;
using Finanzas.Domain.Transactions;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Transactions;

public class TransactionTests
{
    [Fact]
    public void Un_previsto_se_confirma_con_el_monto_real()
    {
        var luz = Transaction.CreateExpense(
            Guid.NewGuid(), Banco(), Fecha(2026, 10, 10), Money.Ars(20_000), Gasto("Servicios"), "Luz",
            status: TransactionStatus.Planned).Value;

        Assert.True(luz.Confirm(Money.Ars(23_500)).IsSuccess);

        Assert.Equal(-23_500m, luz.Amount);
        Assert.Equal(TransactionStatus.Confirmed, luz.Status);
        Assert.Equal(TransactionErrors.AlreadyConfirmed, luz.Confirm().Error);
    }

    [Fact]
    public void El_faltante_se_registra_como_ajuste_por_la_diferencia()
    {
        // En el Excel figura como un gasto "faltante" de $15.000.
        var ajuste = Transaction.CreateReconciliation(
            Guid.NewGuid(), MercadoPago(), Fecha(2025, 9, 16),
            calculatedBalance: Money.Ars(120_000), realBalance: Money.Ars(105_000), Ajuste()).Value;

        Assert.Equal(TransactionKind.Adjustment, ajuste.Kind);
        Assert.Equal(-15_000m, ajuste.Amount);
    }

    [Fact]
    public void Si_el_saldo_coincide_no_hay_nada_que_ajustar()
    {
        var result = Transaction.CreateReconciliation(
            Guid.NewGuid(), MercadoPago(), Fecha(2025, 9, 16), Money.Ars(120_000), Money.Ars(120_000), Ajuste());

        Assert.Equal(TransactionErrors.NothingToAdjust, result.Error);
    }

    [Fact]
    public void Los_rendimientos_van_solo_en_cuentas_que_generan_intereses()
    {
        // Abril de 2026: $13.761 de rendimientos en el ahorro.
        Assert.True(Transaction.CreateYield(Guid.NewGuid(), Ahorro(), Fecha(2026, 4, 30), Money.Ars(13_761)).IsSuccess);

        var enElBanco = Transaction.CreateYield(Guid.NewGuid(), Banco(), Fecha(2026, 4, 30), Money.Ars(13_761));

        Assert.Equal(TransactionErrors.AccountDoesNotEarnYield, enElBanco.Error);
    }

    [Fact]
    public void Un_gasto_lleva_categoria_y_etiquetas_por_separado()
    {
        // "helado con lu": la categoría es Helado y la etiqueta es Lu.
        var helado = Transaction.CreateExpense(
            Guid.NewGuid(), MercadoPago(), Fecha(2025, 9, 13), Money.Ars(8_800), Gasto("Helado"), "helado con lu").Value;
        var lu = Etiqueta("Lu");

        Assert.True(helado.SetTags([lu, lu]).IsSuccess);

        Assert.Equal(lu.Id, Assert.Single(helado.TagIds));
    }

    [Fact]
    public void No_acepta_etiquetas_de_otro_libro()
    {
        var helado = Transaction.CreateExpense(
            Guid.NewGuid(), MercadoPago(), Fecha(2025, 9, 13), Money.Ars(8_800), Gasto("Helado")).Value;
        var deOtroLibro = Tag.Create(Guid.NewGuid(), Guid.NewGuid(), "Lu").Value;

        Assert.Equal(TransactionErrors.TagFromAnotherLedger, helado.SetTags([deOtroLibro]).Error);
    }

    [Fact]
    public void Un_gasto_no_acepta_una_categoria_de_ingreso()
    {
        var result = Transaction.CreateExpense(Guid.NewGuid(), Banco(), Fecha(2025, 9, 13), Money.Ars(8_800), Ingreso());

        Assert.Equal(TransactionErrors.CategoryKindMismatch, result.Error);
    }

    [Fact]
    public void No_se_registran_ingresos_en_una_tarjeta()
    {
        var result = Transaction.CreateIncome(Guid.NewGuid(), Visa(), Fecha(2026, 6, 30), Money.Ars(500_000), Ingreso());

        Assert.Equal(TransactionErrors.IncomeOnCredit, result.Error);
    }
}
