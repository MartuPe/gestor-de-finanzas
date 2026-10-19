using Finanzas.Application.Abstractions;
using Finanzas.Application.CreditLines.GetDebtSchedule;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Reports.GetSpending;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;

namespace Finanzas.Infrastructure.Tests.UseCases;

/// <summary>Gasto por categoría o etiqueta (R10), deuda por tarjeta y fijos a demanda.</summary>
public class ReportsTests(SqlServerFixture fixture)
{
    private static readonly Period Septiembre = new(2025, 9);
    private static readonly Period Octubre = new(2025, 10);

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task En_modo_vencimiento_cada_cuota_cuenta_en_el_mes_en_que_se_paga()
    {
        var (data, app) = await CargarSeptiembre2025Async();
        await using var _ = app;

        var septiembre = (await app.GetSpendingAsync(new GetSpendingQuery(Septiembre), _ct)).Value;
        var octubre = (await app.GetSpendingAsync(new GetSpendingQuery(Octubre), _ct)).Value;

        // En septiembre solo cuenta el helado pagado con Mercado Pago: la primera cuota del perfume vence en octubre.
        var helado = Assert.Single(septiembre.Groups);
        Assert.Equal(("Helado", 8_800m), (helado.Name, helado.Amounts[CurrencyCodes.Ars]));
        var regalos = Assert.Single(octubre.Groups);
        Assert.Equal((data.Regalos.Id, 14_000m), (regalos.Id!.Value, regalos.Amounts[CurrencyCodes.Ars]));
    }

    [Fact]
    public async Task En_modo_compra_la_compra_cuenta_entera_en_el_mes_en_que_se_hizo()
    {
        var (_, app) = await CargarSeptiembre2025Async();
        await using var __ = app;

        var septiembre = (await app.GetSpendingAsync(new GetSpendingQuery(Septiembre, Mode: SpendingMode.Purchase), _ct)).Value;

        Assert.Equal(
            new[] { ("Regalos", 42_000m), ("Helado", 8_800m) },
            septiembre.Groups.Select(group => (group.Name, group.Unified!.Value)));
    }

    [Fact]
    public async Task Por_etiqueta_se_ve_todo_lo_gastado_con_Lu()
    {
        var (data, app) = await CargarSeptiembre2025Async();
        await using var _ = app;
        await Gastar(app, new RegisterExpenseCommand(
            Guid.NewGuid(), data.MercadoPago.Id, new DateOnly(2025, 9, 16), 15_000m, CurrencyCodes.Ars, data.Varios.Id, "faltante"));

        var septiembre = (await app.GetSpendingAsync(
            new GetSpendingQuery(Septiembre, SpendingGrouping.Tag, SpendingMode.Purchase), _ct)).Value;

        Assert.Equal(
            new[] { ("Lu", 50_800m), ("Sin etiqueta", 15_000m) },
            septiembre.Groups.Select(group => (group.Name, group.Unified!.Value)));
    }

    [Fact]
    public async Task La_deuda_de_la_Visa_mes_a_mes_reproduce_el_ejemplo_del_helado_y_el_perfume()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        await Gastar(app, new RegisterExpenseCommand(
            Guid.NewGuid(), data.Visa.Id, new DateOnly(2025, 9, 6), 9_000m, CurrencyCodes.Ars, data.Helado.Id, "Helado con mamá"));
        await Gastar(app, Perfume(data));

        var deuda = (await app.GetDebtScheduleAsync(new GetDebtScheduleQuery(Octubre, Months: 3), _ct)).Value;

        Assert.Equal(new[] { 23_000m, 14_000m, 14_000m }, deuda.Months.Select(month => month.TotalUnified!.Value));
        Assert.All(deuda.Months, month => Assert.Equal("Visa", Assert.Single(month.Lines).Name));
    }

    [Fact]
    public async Task Abrir_el_mismo_mes_dos_veces_no_duplica_los_fijos()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using (var db = fixture.CreateContext())
        {
            db.RecurringTemplates.AddRange(
                RecurringTemplate.Create(
                    Guid.NewGuid(), "Sueldo", RecurringKind.Income, data.Banco, null, data.Sueldo, Money.Ars(1_800_000),
                    RecurringFrequency.Monthly, interval: 1, dayOfMonth: 1, startsOn: new DateOnly(2026, 5, 1)).Value,
                RecurringTemplate.Create(
                    Guid.NewGuid(), "Ahorro mensual", RecurringKind.Transfer, data.Banco, data.Ahorro, null, Money.Ars(800_000),
                    RecurringFrequency.Monthly, interval: 1, dayOfMonth: 5, startsOn: new DateOnly(2026, 5, 1)).Value);
            await db.SaveChangesAsync(_ct);
        }

        await using var app = App.For(fixture, data);
        var primera = (await app.GenerateRecurringAsync(new Period(2026, 10), _ct)).Value;
        var segunda = (await app.GenerateRecurringAsync(new Period(2026, 10), _ct)).Value;

        Assert.Equal(3, primera.Generated); // sueldo + las dos patas del ahorro
        Assert.Equal(0, segunda.Generated);
    }

    /// <summary>Perfume Lu en 3 cuotas con la Visa y un helado con Mercado Pago, los dos con la etiqueta Lu.</summary>
    private async Task<(TestLedger Data, App App)> CargarSeptiembre2025Async()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        var app = App.For(fixture, data);
        await Gastar(app, Perfume(data));
        await Gastar(app, new RegisterExpenseCommand(
            Guid.NewGuid(), data.MercadoPago.Id, new DateOnly(2025, 9, 13), 8_800m, CurrencyCodes.Ars, data.Helado.Id,
            "helado con lu", TagIds: [data.Lu.Id]));
        return (data, app);
    }

    private static RegisterExpenseCommand Perfume(TestLedger data) =>
        new(Guid.NewGuid(), data.Visa.Id, new DateOnly(2025, 9, 10), 42_000m, CurrencyCodes.Ars, data.Regalos.Id,
            "Perfume Lu", Installments: 3, TagIds: [data.Lu.Id]);

    private async Task Gastar(App app, RegisterExpenseCommand command) =>
        _ = (await app.RegisterExpenseAsync(command, _ct)).Value;
}
