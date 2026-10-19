using Finanzas.Application.Common;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Tests.UseCases;

public class RegisterExpenseTests(SqlServerFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Una_compra_en_cuotas_devuelve_en_que_resumen_cae_cada_cuota()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);

        var compra = (await app.RegisterExpenseAsync(Perfume(data), _ct)).Value;

        Assert.True(compra.Created);
        Assert.Equal(
            new Period?[] { new(2025, 10), new(2025, 11), new(2025, 12) },
            compra.Value.Installments.Select(cuota => cuota.StatementPeriod));
        Assert.Equal(
            new DateOnly?[] { new(2025, 10, 5), new(2025, 11, 5), new(2025, 12, 5) },
            compra.Value.Installments.Select(cuota => cuota.DueDate));
        Assert.Equal(data.Lu.Id, Assert.Single(compra.Value.TagIds));
    }

    [Fact]
    public async Task Si_el_celular_reintenta_el_mismo_gasto_no_se_duplica()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        var command = Perfume(data);

        var primero = (await app.RegisterExpenseAsync(command, _ct)).Value;
        var reintento = (await app.RegisterExpenseAsync(command, _ct)).Value;

        Assert.False(reintento.Created);
        Assert.Equal(primero.Value.Id, reintento.Value.Id);
        Assert.Equal(new Period(2025, 10), reintento.Value.Installments[0].StatementPeriod);
        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.Transactions.CountAsync(transaction => transaction.LedgerId == data.Ledger.Id, _ct));
    }

    [Fact]
    public async Task El_mismo_Id_con_otros_datos_es_un_conflicto()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        var command = Perfume(data);
        await app.RegisterExpenseAsync(command, _ct);

        var result = await app.RegisterExpenseAsync(command with { Amount = 50_000m }, _ct);

        Assert.Equal(ApplicationErrors.IdInUse, result.Error);
    }

    [Fact]
    public async Task No_se_puede_usar_una_cuenta_de_otro_libro()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        var otroLibro = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);

        var result = await app.RegisterExpenseAsync(Perfume(data) with { AccountId = otroLibro.Visa.Id }, _ct);

        Assert.Equal(ApplicationErrors.AccountNotFound, result.Error);
    }

    [Fact]
    public async Task Los_datos_mal_formados_vuelven_con_el_detalle_por_campo()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);

        var result = await app.RegisterExpenseAsync(
            Perfume(data) with { Amount = 10.005m, Currency = "ars", Installments = 0 }, _ct);

        var invalid = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(
            new[] { "Amount", "Currency", "Installments" },
            invalid.Fields.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Las_cuotas_solo_existen_en_tarjetas_y_creditos()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);

        var result = await app.RegisterExpenseAsync(Perfume(data) with { AccountId = data.MercadoPago.Id }, _ct);

        Assert.Equal("transaction.plan_not_allowed", result.Error?.Code);
    }

    private static RegisterExpenseCommand Perfume(TestLedger data) =>
        new(Guid.NewGuid(), data.Visa.Id, new DateOnly(2025, 9, 10), 42_000m, CurrencyCodes.Ars, data.Regalos.Id,
            "Perfume Lu", Installments: 3, TagIds: [data.Lu.Id]);
}
