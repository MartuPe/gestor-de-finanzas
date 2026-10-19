using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using Finanzas.Domain.Transactions;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Transactions;

public class CreditPurchaseTests
{
    [Fact]
    public void Una_compra_en_cuotas_se_guarda_en_negativo_y_sus_cuotas_suman_el_total()
    {
        var visa = Visa();
        var plan = InstallmentScheduler.Plan(visa, Fecha(2025, 9, 10), Money.Ars(42_000), 3, []).Value;

        var compra = Transaction.CreateExpense(
            Guid.NewGuid(), visa, Fecha(2025, 9, 10), Money.Ars(42_000), Gasto("Regalos"), "Perfume Lu", plan).Value;

        Assert.Equal(-42_000m, compra.Amount);
        Assert.Equal(new[] { "1/3", "2/3", "3/3" }, compra.Installments.Select(cuota => cuota.Label));
        Assert.Equal(42_000m, compra.Installments.Sum(cuota => cuota.Amount));
        Assert.Equal(
            plan.Installments.Select(planned => planned.Statement.Id),
            compra.Installments.Select(cuota => cuota.StatementId));
    }

    [Fact]
    public void Un_gasto_con_tarjeta_necesita_sus_cuotas()
    {
        var result = Transaction.CreateExpense(Guid.NewGuid(), Visa(), Fecha(2025, 9, 6), Money.Ars(9_000), Gasto());

        Assert.Equal(TransactionErrors.PlanRequiredForCredit, result.Error);
    }

    [Fact]
    public void Un_gasto_con_Mercado_Pago_no_lleva_cuotas()
    {
        var plan = InstallmentScheduler.Plan(Visa(), Fecha(2025, 9, 6), Money.Ars(9_000), 1, []).Value;

        var result = Transaction.CreateExpense(Guid.NewGuid(), MercadoPago(), Fecha(2025, 9, 6), Money.Ars(9_000), Gasto(), plan: plan);

        Assert.Equal(TransactionErrors.PlanOnlyForCredit, result.Error);
    }

    [Fact]
    public void Las_cuotas_tienen_que_sumar_el_monto_de_la_compra()
    {
        var visa = Visa();
        var plan = InstallmentScheduler.Plan(visa, Fecha(2025, 9, 10), Money.Ars(40_000), 3, []).Value;

        var result = Transaction.CreateExpense(Guid.NewGuid(), visa, Fecha(2025, 9, 10), Money.Ars(42_000), Gasto(), plan: plan);

        Assert.Equal(TransactionErrors.PlanTotalMismatch, result.Error);
    }

    [Fact]
    public void Las_cuotas_se_pueden_editar_y_el_total_se_recalcula()
    {
        // "Perfume mamá": 30.000, 30.000 y 31.600.
        var compra = Compra(Mastercard(), Fecha(2025, 10, 15), 90_000, 3);

        var result = compra.EditInstallmentAmounts([30_000m, 30_000m, 31_600m]);

        Assert.True(result.IsSuccess);
        Assert.Equal(-91_600m, compra.Amount);
        Assert.Equal(
            new[] { 30_000m, 30_000m, 31_600m },
            compra.Installments.OrderBy(cuota => cuota.Number).Select(cuota => cuota.Amount));
    }

    [Fact]
    public void Una_cuota_editada_no_puede_cambiar_de_signo()
    {
        var compra = Compra(Mastercard(), Fecha(2025, 10, 15), 90_000, 3);

        var result = compra.EditInstallmentAmounts([30_000m, -30_000m, 30_000m]);

        Assert.Equal(TransactionErrors.InvalidInstallmentAmount, result.Error);
        Assert.Equal(-90_000m, compra.Amount);
    }

    [Fact]
    public void Una_devolucion_con_tarjeta_es_una_cuota_negativa_en_el_resumen()
    {
        var visa = Visa();
        var plan = InstallmentScheduler.Plan(visa, Fecha(2025, 9, 20), Money.Ars(-9_000), 1, []).Value;

        var devolucion = Transaction.CreateRefund(
            Guid.NewGuid(), visa, Fecha(2025, 9, 20), Money.Ars(9_000), Gasto("Ropa"), "Devolución remera", plan).Value;

        Assert.Equal(9_000m, devolucion.Amount);
        Assert.Equal(-9_000m, Assert.Single(devolucion.Installments).Amount);
    }

    private static Transaction Compra(Account account, DateOnly date, decimal amount, int cuotas)
    {
        var plan = InstallmentScheduler.Plan(account, date, Money.Ars(amount), cuotas, []).Value;
        return Transaction.CreateExpense(Guid.NewGuid(), account, date, Money.Ars(amount), Gasto(), plan: plan).Value;
    }
}
