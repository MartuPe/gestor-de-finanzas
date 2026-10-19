using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Services;

public class RescheduleTests
{
    [Fact]
    public void Si_el_cierre_se_adelanta_la_compra_pasa_al_resumen_siguiente()
    {
        var visa = Visa();
        var plan = InstallmentScheduler.Plan(visa, Fecha(2025, 9, 26), Money.Ars(20_000), 2, []).Value;
        var compra = Transaction.CreateExpense(Guid.NewGuid(), visa, Fecha(2025, 9, 26), Money.Ars(20_000), Gasto(), plan: plan).Value;
        var octubre = plan.Installments[0].Statement;
        var noviembre = plan.Installments[1].Statement;

        // El resumen real de octubre cerró el 25 y no el 28: la compra del 26 ya no entra.
        Assert.True(octubre.ChangeDates(visa, Fecha(2025, 9, 25), Fecha(2025, 10, 5), hasPayments: false).IsSuccess);
        var nuevos = InstallmentScheduler.Reschedule(compra, visa, plan.NewStatements, new HashSet<Guid>()).Value;

        var diciembre = Assert.Single(nuevos);
        Assert.Equal(Mes(2025, 12), diciembre.Period);
        Assert.Equal(new[] { noviembre.Id, diciembre.Id }, StatementIds(compra));
    }

    [Fact]
    public void No_mueve_cuotas_hacia_un_resumen_que_ya_tiene_pagos()
    {
        var visa = Visa();
        var plan = InstallmentScheduler.Plan(visa, Fecha(2025, 9, 26), Money.Ars(20_000), 2, []).Value;
        var compra = Transaction.CreateExpense(Guid.NewGuid(), visa, Fecha(2025, 9, 26), Money.Ars(20_000), Gasto(), plan: plan).Value;
        var octubre = plan.Installments[0].Statement;
        var noviembre = plan.Installments[1].Statement;
        var antes = StatementIds(compra);

        octubre.ChangeDates(visa, Fecha(2025, 9, 25), Fecha(2025, 10, 5), hasPayments: false);
        var result = InstallmentScheduler.Reschedule(compra, visa, plan.NewStatements, new HashSet<Guid> { noviembre.Id });

        Assert.Equal(StatementErrors.HasPayments, result.Error);
        Assert.Equal(antes, StatementIds(compra));
    }

    [Fact]
    public void Un_resumen_con_pagos_no_cambia_sus_fechas()
    {
        var visa = Visa();
        var octubre = Statement.Create(Guid.NewGuid(), visa, Mes(2025, 10), Fecha(2025, 9, 28), Fecha(2025, 10, 5)).Value;

        var result = octubre.ChangeDates(visa, Fecha(2025, 9, 25), Fecha(2025, 10, 5), hasPayments: true);

        Assert.Equal(StatementErrors.HasPayments, result.Error);
        Assert.Equal(Fecha(2025, 9, 28), octubre.ClosingDate);
    }

    private static List<Guid> StatementIds(Transaction compra) =>
        compra.Installments.OrderBy(cuota => cuota.Number).Select(cuota => cuota.StatementId).ToList();
}
