using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Services;

public class InstallmentSchedulerTests
{
    [Fact]
    public void Reparte_en_partes_iguales_y_el_redondeo_va_a_la_ultima()
    {
        Assert.Equal(
            new[] { Money.Ars(14_000), Money.Ars(14_000), Money.Ars(14_000) },
            InstallmentScheduler.Split(Money.Ars(42_000), 3));
        Assert.Equal(
            new[] { Money.Ars(33.33m), Money.Ars(33.33m), Money.Ars(33.34m) },
            InstallmentScheduler.Split(Money.Ars(100), 3));
    }

    [Fact]
    public void El_helado_del_6_de_septiembre_con_Visa_cae_en_el_resumen_que_vence_en_octubre()
    {
        var plan = Plan(Visa(), Fecha(2025, 9, 6), 9_000, 1);

        var cuota = Assert.Single(plan.Installments);
        Assert.Equal((1, 1), (cuota.Number, cuota.Total));
        Assert.Equal(Mes(2025, 10), cuota.Statement.Period);
        Assert.Equal(Fecha(2025, 9, 28), cuota.Statement.ClosingDate);
        Assert.Equal(Fecha(2025, 10, 5), cuota.Statement.DueDate);
        Assert.True(cuota.Statement.IsEstimated);
        Assert.Single(plan.NewStatements);
    }

    [Fact]
    public void Perfume_Lu_en_3_cuotas_cae_en_octubre_noviembre_y_diciembre()
    {
        var plan = Plan(Visa(), Fecha(2025, 9, 10), 42_000, 3);

        Assert.Equal(new[] { 1, 2, 3 }, plan.Installments.Select(cuota => cuota.Number));
        Assert.Equal(new[] { Mes(2025, 10), Mes(2025, 11), Mes(2025, 12) }, Periods(plan));
        Assert.All(plan.Installments, cuota => Assert.Equal(Money.Ars(14_000), cuota.Amount));
        Assert.Equal(3, plan.NewStatements.Count);
    }

    [Fact]
    public void Dos_compras_del_mismo_mes_comparten_el_resumen()
    {
        var visa = Visa();
        var helado = Plan(visa, Fecha(2025, 9, 6), 9_000, 1);

        var perfume = InstallmentScheduler.Plan(visa, Fecha(2025, 9, 10), Money.Ars(42_000), 3, helado.NewStatements).Value;

        Assert.Same(helado.Installments[0].Statement, perfume.Installments[0].Statement);
        Assert.Equal(2, perfume.NewStatements.Count);
    }

    [Fact]
    public void Una_compra_posterior_al_cierre_pasa_al_resumen_siguiente()
    {
        var plan = Plan(Visa(), Fecha(2025, 9, 29), 9_000, 1);

        Assert.Equal(Mes(2025, 11), Assert.Single(plan.Installments).Statement.Period);
    }

    [Fact]
    public void Una_compra_del_dia_del_cierre_entra_en_ese_resumen()
    {
        var plan = Plan(Visa(), Fecha(2025, 9, 28), 9_000, 1);

        Assert.Equal(Mes(2025, 10), Assert.Single(plan.Installments).Statement.Period);
    }

    [Fact]
    public void En_Mercado_Credito_la_primera_cuota_vence_el_mes_siguiente()
    {
        // "Elementos de gym (1/3)": se compró en mayo y la primera cuota se pagó en junio.
        var plan = Plan(MercadoCredito(), Fecha(2026, 5, 15), 84_000, 3);

        Assert.Equal(new[] { Mes(2026, 6), Mes(2026, 7), Mes(2026, 8) }, Periods(plan));
        Assert.All(plan.Installments, cuota => Assert.Null(cuota.Statement.ClosingDate));
    }

    [Fact]
    public void Respeta_un_resumen_real_aunque_tenga_otras_fechas()
    {
        // En enero de 2026 cambiaron el cierre y el vencimiento de las tarjetas.
        var visa = Visa();
        var febrero = Statement.Create(Guid.NewGuid(), visa, Mes(2026, 2), Fecha(2026, 2, 2), Fecha(2026, 2, 12)).Value;

        var plan = InstallmentScheduler.Plan(visa, Fecha(2026, 1, 30), Money.Ars(15_000), 1, [febrero]).Value;

        Assert.Same(febrero, Assert.Single(plan.Installments).Statement);
        Assert.Empty(plan.NewStatements);
    }

    [Fact]
    public void Una_compra_importada_en_curso_arranca_en_su_cuota()
    {
        // "Perfume Lu (2/3)" ya venía en curso cuando empieza el Excel, en septiembre de 2025.
        var plan = InstallmentScheduler.Plan(Visa(), Fecha(2025, 8, 20), Money.Ars(28_000), 3, [], firstNumber: 2).Value;

        Assert.Equal(new[] { 2, 3 }, plan.Installments.Select(cuota => cuota.Number));
        Assert.All(plan.Installments, cuota => Assert.Equal(3, cuota.Total));
        Assert.Equal(new[] { Mes(2025, 9), Mes(2025, 10) }, Periods(plan));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Rechaza_una_cantidad_de_cuotas_fuera_de_rango(int cuotas)
    {
        var result = InstallmentScheduler.Plan(Visa(), Fecha(2025, 9, 6), Money.Ars(9_000), cuotas, []);

        Assert.Equal(InstallmentErrors.InvalidCount, result.Error);
    }

    [Fact]
    public void Una_devolucion_va_en_un_solo_pago()
    {
        var result = InstallmentScheduler.Plan(Visa(), Fecha(2025, 9, 6), Money.Ars(-9_000), 3, []);

        Assert.Equal(InstallmentErrors.RefundInOnePayment, result.Error);
    }

    [Fact]
    public void Una_cuenta_de_dinero_no_tiene_cuotas()
    {
        var result = InstallmentScheduler.Plan(Banco(), Fecha(2025, 9, 6), Money.Ars(9_000), 1, []);

        Assert.Equal(InstallmentErrors.NotCreditAccount, result.Error);
    }

    private static InstallmentPlan Plan(Account account, DateOnly date, decimal amount, int installments) =>
        InstallmentScheduler.Plan(account, date, Money.Ars(amount), installments, []).Value;

    private static IEnumerable<Period> Periods(InstallmentPlan plan) =>
        plan.Installments.Select(cuota => cuota.Statement.Period);
}
