using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Services;
using Finanzas.Domain.Transactions;
using Finanzas.Infrastructure.Persistence;
using Finanzas.Infrastructure.Persistence.Conventions;
using Finanzas.Infrastructure.Persistence.Records;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Tests;

public class PersistenceTests(SqlServerFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Una_compra_en_cuotas_se_guarda_y_se_lee_completa()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        var compraId = await GuardarCompraAsync(data, Money.Ars(42_000), cuotas: 3);

        await using var db = fixture.CreateContext();
        var compra = await db.Transactions.SingleAsync(transaction => transaction.Id == compraId, _ct);

        Assert.Equal(-42_000m, compra.Amount);
        Assert.Equal(new[] { "1/3", "2/3", "3/3" }, compra.Installments.OrderBy(cuota => cuota.Number).Select(cuota => cuota.Label));
        Assert.Equal(data.Lu.Id, Assert.Single(compra.TagIds));

        var periodos = await db.Statements
            .Where(statement => statement.CreditAccountId == data.Visa.Id)
            .OrderBy(statement => statement.Period)
            .Select(statement => statement.Period)
            .ToListAsync(_ct);
        Assert.Equal(new[] { new Period(2025, 10), new Period(2025, 11), new Period(2025, 12) }, periodos);

        var creado = db.Entry(compra).Property<DateTime>(AuditColumns.CreatedAt).CurrentValue;
        Assert.InRange(creado, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Los_enums_se_guardan_en_snake_case_como_dice_el_modelo()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        var compraId = await GuardarCompraAsync(data, Money.Ars(9_000), cuotas: 1);

        await using var db = fixture.CreateContext();

        var kind = await db.Database
            .SqlQuery<string>($"SELECT [Kind] AS [Value] FROM [Transactions] WHERE [Id] = {compraId}")
            .SingleAsync(_ct);
        var lineType = await db.Database
            .SqlQuery<string>($"SELECT [LineType] AS [Value] FROM [CreditLines] WHERE [AccountId] = {data.Visa.Id}")
            .SingleAsync(_ct);

        Assert.Equal("expense", kind);
        Assert.Equal("card", lineType);
    }

    [Fact]
    public async Task Borrar_un_movimiento_lo_oculta_pero_no_lo_elimina()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        var compraId = await GuardarCompraAsync(data, Money.Ars(20_000), cuotas: 2);

        await using (var db = fixture.CreateContext())
        {
            db.Transactions.Remove(await db.Transactions.SingleAsync(transaction => transaction.Id == compraId, _ct));
            await db.SaveChangesAsync(_ct);
        }

        await using var verificacion = fixture.CreateContext();
        Assert.False(await verificacion.Transactions.AnyAsync(transaction => transaction.Id == compraId, _ct));
        Assert.False(await verificacion.Installments.AnyAsync(cuota => cuota.TransactionId == compraId, _ct));

        var borrada = await verificacion.Transactions
            .IgnoreQueryFilters()
            .SingleAsync(transaction => transaction.Id == compraId, _ct);
        Assert.NotNull(verificacion.Entry(borrada).Property<DateTime?>(AuditColumns.DeletedAt).CurrentValue);
        Assert.Equal(2, borrada.Installments.Count);

        // La etiqueta sigue vinculada: un "deshacer" la recupera.
        Assert.Equal(data.Lu.Id, Assert.Single(borrada.TagIds));
    }

    [Fact]
    public async Task Un_fijo_borrado_no_se_vuelve_a_generar_ese_mes()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        var youtube = RecurringTemplate.Create(
            Guid.NewGuid(), "YouTube Premium", RecurringKind.Expense, data.Banco, null, data.Suscripciones, Money.Ars(10_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 15, startsOn: new DateOnly(2026, 1, 1)).Value;
        var octubre = new Period(2026, 10);

        await using (var db = fixture.CreateContext())
        {
            db.RecurringTemplates.Add(youtube);
            db.Transactions.AddRange(Generar(data, youtube, octubre).Transactions);
            await db.SaveChangesAsync(_ct);

            db.Transactions.Remove(await db.Transactions.SingleAsync(transaction => transaction.RecurringTemplateId == youtube.Id, _ct));
            await db.SaveChangesAsync(_ct);
        }

        // Aunque un job se olvide de lo ya generado, la base no deja duplicarlo.
        await using var otraVez = fixture.CreateContext();
        otraVez.Transactions.AddRange(Generar(data, youtube, octubre).Transactions);

        await Assert.ThrowsAsync<DbUpdateException>(() => otraVez.SaveChangesAsync(_ct));
    }

    [Fact]
    public async Task La_base_rechaza_un_gasto_con_monto_positivo()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var db = fixture.CreateContext();

        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO [Transactions] ([Id], [LedgerId], [AccountId], [Date], [Amount], [Currency], [Kind], [Status])
            VALUES ({Guid.NewGuid()}, {data.Ledger.Id}, {data.Banco.Id}, '2025-09-06', 9000, 'ARS', 'expense', 'confirmed')
            """,
            _ct));

        Assert.Contains("CK_Transactions_AmountSign", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dos_ediciones_a_la_vez_no_se_pisan()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var celular = fixture.CreateContext();
        await using var compu = fixture.CreateContext();
        var enCelular = await celular.Accounts.SingleAsync(account => account.Id == data.Banco.Id, _ct);
        var enCompu = await compu.Accounts.SingleAsync(account => account.Id == data.Banco.Id, _ct);

        Assert.True(enCelular.Rename("Banco (desde el celular)").IsSuccess);
        await celular.SaveChangesAsync(_ct);
        Assert.True(enCompu.Rename("Banco (desde la compu)").IsSuccess);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => compu.SaveChangesAsync(_ct));
    }

    [Fact]
    public async Task Hay_una_sola_cotizacion_por_dia_y_casa()
    {
        await using var db = fixture.CreateContext();
        var fecha = new DateOnly(2001, 1, 1).AddDays(Random.Shared.Next(0, 9_000));
        db.ExchangeRates.AddRange(Blue(fecha, 1_535m, 1_555m), Blue(fecha, 1_540m, 1_560m));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(_ct));
    }

    private async Task<Guid> GuardarCompraAsync(TestLedger data, Money monto, int cuotas)
    {
        var fecha = new DateOnly(2025, 9, 10);
        var plan = InstallmentScheduler.Plan(data.Visa, fecha, monto, cuotas, []).Value;
        var compra = Transaction.CreateExpense(Guid.NewGuid(), data.Visa, fecha, monto, data.Regalos, "Perfume Lu", plan).Value;
        Assert.True(compra.SetTags([data.Lu]).IsSuccess);

        await using var db = fixture.CreateContext();
        db.Statements.AddRange(plan.NewStatements);
        db.Transactions.Add(compra);
        await db.SaveChangesAsync(_ct);
        return compra.Id;
    }

    private static RecurringBatch Generar(TestLedger data, RecurringTemplate template, Period period) =>
        RecurringGenerator.Generate(
            [template],
            period,
            new[] { data.Banco }.ToDictionary(account => account.Id),
            new[] { data.Suscripciones }.ToDictionary(category => category.Id),
            [],
            new HashSet<GeneratedKey>());

    private static ExchangeRateRecord Blue(DateOnly fecha, decimal compra, decimal venta) => new()
    {
        Currency = CurrencyCodes.Usd,
        Source = ExchangeRateSources.Blue,
        RateDate = fecha,
        Buy = compra,
        Sell = venta,
        FetchedAt = DateTime.UtcNow,
    };
}
