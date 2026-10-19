using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Statements.PayStatement;
using Finanzas.Application.Transactions.ConfirmPlanned;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;

namespace Finanzas.Infrastructure.Tests.UseCases;

/// <summary>La hoja Octubre2026 del Excel, cargada con los casos de uso y leída como la pantalla principal.</summary>
public class MonthSummaryTests(SqlServerFixture fixture)
{
    private static readonly Period Octubre = new(2026, 10);
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(App.Today.DateTime);

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Octubre_2026_con_el_blue_del_dia()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        await CargarOctubre2026Async(data, app, blue: 1_555m);

        var mes = (await app.GetMonthSummaryAsync(Octubre, _ct)).Value;

        Assert.Equal(460_000m, mes.Available[CurrencyCodes.Ars]);
        Assert.Equal(-40m, mes.Available[CurrencyCodes.Usd]);
        Assert.Equal(397_800m, mes.AvailableUnified); // 460.000 − 40 × 1.555
        Assert.Equal(1_555m, Assert.Single(mes.Rates).Sell);

        Assert.Equal(3, mes.StatementsDue.Count);
        var patagonia = Assert.Single(mes.StatementsDue, statement => statement.CreditAccountName == "Patagonia");
        Assert.Equal(50_000m, patagonia.Outstanding[CurrencyCodes.Ars]);
        Assert.Equal(40m, patagonia.Outstanding[CurrencyCodes.Usd]);

        var ahorro = Assert.Single(mes.Planned);
        Assert.Equal((-800_000m, new DateOnly(2026, 10, 5)), (ahorro.Amount, ahorro.Date));
        Assert.True(ahorro.IsOverdue);
    }

    [Fact]
    public async Task A_1500_da_lo_mismo_que_la_celda_K10_del_Excel()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        await CargarOctubre2026Async(data, app, blue: 1_500m);

        var mes = (await app.GetMonthSummaryAsync(Octubre, _ct)).Value;

        Assert.Equal(400_000m, mes.AvailableUnified);
    }

    [Fact]
    public async Task Pagar_un_resumen_no_cambia_el_disponible()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        await CargarOctubre2026Async(data, app, blue: 1_555m);
        var antes = (await app.GetMonthSummaryAsync(Octubre, _ct)).Value;
        var mastercard = Assert.Single(antes.StatementsDue, statement => statement.CreditAccountName == "Mastercard");

        var pago = (await app.PayStatementAsync(
            new PayStatementCommand(
                Guid.NewGuid(), Guid.NewGuid(), mastercard.StatementId, data.Banco.Id, new DateOnly(2026, 10, 5),
                75_000m, CurrencyCodes.Ars, 75_000m, CurrencyCodes.Ars),
            _ct)).Value;
        var despues = (await app.GetMonthSummaryAsync(Octubre, _ct)).Value;

        Assert.Equal(StatementStatus.Paid, pago.Value.Status);
        Assert.Equal(antes.AvailableUnified, despues.AvailableUnified);
        Assert.DoesNotContain(despues.StatementsDue, statement => statement.CreditAccountName == "Mastercard");
    }

    [Fact]
    public async Task Si_el_ahorro_se_confirma_por_menos_el_disponible_sube()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        await CargarOctubre2026Async(data, app, blue: 1_555m);
        var ahorro = Assert.Single((await app.GetMonthSummaryAsync(Octubre, _ct)).Value.Planned);

        var patas = (await app.ConfirmPlannedAsync(new ConfirmPlannedCommand(ahorro.TransactionId, 700_000m), _ct)).Value;
        var mes = (await app.GetMonthSummaryAsync(Octubre, _ct)).Value;

        Assert.Equal(2, patas.Count);
        Assert.All(patas, pata => Assert.Equal(TransactionStatus.Confirmed, pata.Status));
        Assert.Empty(mes.Planned);
        Assert.Equal(560_000m, mes.Available[CurrencyCodes.Ars]);
    }

    /// <summary>
    /// Lo que hay en la hoja Octubre2026: 1.800.000 en el banco, 406.000 de gastos variables, el ahorro de 800.000
    /// como fijo, y los resúmenes que vencen en octubre (Mastercard, Patagonia en pesos y dólares, Mercado Crédito).
    /// </summary>
    private async Task CargarOctubre2026Async(TestLedger data, App app, decimal blue)
    {
        await using (var db = fixture.CreateContext())
        {
            db.Transactions.Add(Transaction.CreateOpening(Guid.NewGuid(), data.Banco, new DateOnly(2026, 9, 30), Money.Ars(1_800_000)).Value);
            db.RecurringTemplates.Add(RecurringTemplate.Create(
                Guid.NewGuid(), "Ahorro mensual", RecurringKind.Transfer, data.Banco, data.Ahorro, null, Money.Ars(800_000),
                RecurringFrequency.Monthly, interval: 1, dayOfMonth: 5, startsOn: new DateOnly(2026, 5, 1)).Value);
            await db.SaveChangesAsync(_ct);
        }

        await app.SetUsdRateAsync("blue", Hoy, blue, App.Today, _ct);

        await Gastar(app, data.MercadoPago, new DateOnly(2026, 10, 10), 406_000m, CurrencyCodes.Ars, data.Varios, "Gastos variables");
        await Gastar(app, data.Mastercard, new DateOnly(2026, 9, 10), 75_000m, CurrencyCodes.Ars, data.Varios, "Resumen Mastercard");
        await Gastar(app, data.Patagonia, new DateOnly(2026, 9, 10), 50_000m, CurrencyCodes.Ars, data.Regalos, "Ropa papá (2/3)");
        await Gastar(app, data.Patagonia, new DateOnly(2026, 9, 12), 40m, CurrencyCodes.Usd, data.Suscripciones, "Claude");
        await Gastar(app, data.MercadoCredito, new DateOnly(2026, 9, 15), 9_000m, CurrencyCodes.Ars, data.Varios, "Antecedentes penales");

        Assert.Equal(2, (await app.GenerateRecurringAsync(Octubre, _ct)).Value.Generated);
    }

    private async Task Gastar(App app, Account account, DateOnly date, decimal amount, string currency, Category category, string description) =>
        _ = (await app.RegisterExpenseAsync(
            new RegisterExpenseCommand(Guid.NewGuid(), account.Id, date, amount, currency, category.Id, description),
            _ct)).Value;
}
