using Finanzas.Application.Accounts.CreateAccount;
using Finanzas.Application.Accounts.CreateCreditAccount;
using Finanzas.Application.Accounts.SetOpeningBalance;
using Finanzas.Application.Categories.CreateCategory;
using Finanzas.Application.Common;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Ledgers.CreateLedger;
using Finanzas.Application.Recurring.CreateRecurringTemplate;
using Finanzas.Application.Recurring.UpdateRecurringTemplate;
using Finanzas.Application.Tags.SaveTag;
using Finanzas.Application.Transactions.ListTransactions;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;

namespace Finanzas.Infrastructure.Tests.UseCases;

/// <summary>El alta de un libro desde cero y sus catálogos, como lo va a hacer la app.</summary>
public class CatalogTests(SqlServerFixture fixture)
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Crear_el_libro_carga_las_categorias_iniciales()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;

        var categorias = (await app.ListCategoriesAsync(_ct)).Value;

        Assert.Equal(21, categorias.Count);
        Assert.Contains(categorias, categoria => categoria is { Name: "Helado", Kind: CategoryKind.Expense });
        Assert.Contains(categorias, categoria => categoria is { Name: "Sueldo", Kind: CategoryKind.Income });
        Assert.Equal(
            new[] { "Ajuste", "Rendimientos" },
            categorias.Where(categoria => categoria.IsSystem).Select(categoria => categoria.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Las_cuentas_se_listan_con_su_saldo()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;
        var banco = await CrearBancoAsync(app);
        var visa = (await app.CreateCreditAccountAsync(
            new CreateCreditAccountCommand(Guid.NewGuid(), "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", 28, 5),
            _ct)).Value.Value;
        var helado = (await app.ListCategoriesAsync(_ct)).Value.Single(categoria => categoria.Name == "Helado");

        await app.SetOpeningBalanceAsync(
            new SetOpeningBalanceCommand(Guid.NewGuid(), banco, new DateOnly(2025, 9, 1), 600_000m, CurrencyCodes.Ars), _ct);
        await app.RegisterExpenseAsync(
            new RegisterExpenseCommand(Guid.NewGuid(), visa.Id, new DateOnly(2025, 9, 6), 9_000m, CurrencyCodes.Ars, helado.Id, "helado con mamá"),
            _ct);
        var cuentas = (await app.ListAccountsAsync(_ct)).Value;

        Assert.Equal(600_000m, cuentas.Single(cuenta => cuenta.Id == banco).Balance[CurrencyCodes.Ars]);
        var tarjeta = cuentas.Single(cuenta => cuenta.Id == visa.Id);
        Assert.Equal(-9_000m, tarjeta.Balance[CurrencyCodes.Ars]);
        Assert.Equal((28, 5), (tarjeta.CreditLine!.UsualClosingDay, tarjeta.CreditLine.UsualDueDay));
    }

    [Fact]
    public async Task No_se_repiten_nombres_de_cuenta_ni_saldos_iniciales()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;
        var banco = await CrearBancoAsync(app);
        await app.SetOpeningBalanceAsync(
            new SetOpeningBalanceCommand(Guid.NewGuid(), banco, new DateOnly(2025, 9, 1), 600_000m, CurrencyCodes.Ars), _ct);

        var otroBanco = await app.CreateAccountAsync(
            new CreateAccountCommand(Guid.NewGuid(), "banco supervielle", AccountKind.Bank, CurrencyCodes.Ars), _ct);
        var otroSaldo = await app.SetOpeningBalanceAsync(
            new SetOpeningBalanceCommand(Guid.NewGuid(), banco, new DateOnly(2025, 9, 1), 1_000m, CurrencyCodes.Ars), _ct);

        Assert.Equal(ApplicationErrors.AccountNameTaken, otroBanco.Error);
        Assert.Equal(ApplicationErrors.OpeningExists, otroSaldo.Error);
    }

    [Fact]
    public async Task Una_tarjeta_sin_dia_de_cierre_no_se_crea()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;

        var result = await app.CreateCreditAccountAsync(
            new CreateCreditAccountCommand(Guid.NewGuid(), "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", null, 5),
            _ct);

        Assert.Equal(AccountErrors.CardNeedsClosingDay, result.Error);
    }

    [Fact]
    public async Task Las_subcategorias_cuelgan_de_su_padre_y_los_nombres_no_se_repiten_en_el_mismo_nivel()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;
        var comida = (await app.ListCategoriesAsync(_ct)).Value.Single(categoria => categoria.Name == "Comida");

        var delivery = (await app.CreateCategoryAsync(
            new CreateCategoryCommand(Guid.NewGuid(), "Delivery", CategoryKind.Expense, comida.Id), _ct)).Value.Value;
        var otraComida = await app.CreateCategoryAsync(new CreateCategoryCommand(Guid.NewGuid(), "comida", CategoryKind.Expense), _ct);

        Assert.Equal(comida.Id, delivery.ParentId);
        Assert.Equal(ApplicationErrors.CategoryNameTaken, otraComida.Error);
    }

    [Fact]
    public async Task Una_etiqueta_se_crea_al_vuelo_y_despues_se_renombra()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;
        var id = Guid.NewGuid();

        var creada = (await app.SaveTagAsync(new SaveTagCommand(id, "Pinamar"), _ct)).Value;
        var renombrada = (await app.SaveTagAsync(new SaveTagCommand(id, "Pinamar 2026", "#1D9E75"), _ct)).Value;

        Assert.True(creada.Created);
        Assert.False(renombrada.Created);
        Assert.Equal(("Pinamar 2026", "#1D9E75"), (renombrada.Value.Name, renombrada.Value.Color));
    }

    [Fact]
    public async Task Un_fijo_se_corrige_y_al_pausarlo_deja_de_generarse()
    {
        var (app, _) = await LibroNuevoAsync();
        await using var __ = app;
        var banco = await CrearBancoAsync(app);
        var sueldo = (await app.ListCategoriesAsync(_ct)).Value.Single(categoria => categoria.Name == "Sueldo");
        var fijo = (await app.CreateRecurringTemplateAsync(
            new CreateRecurringTemplateCommand(
                Guid.NewGuid(), "Sueldo", RecurringKind.Income, banco, null, sueldo.Id, 1_800_000m, CurrencyCodes.Ars,
                RecurringFrequency.Monthly, Interval: 1, DayOfMonth: 1, StartsOn: new DateOnly(2026, 5, 1)),
            _ct)).Value.Value;

        var corregido = (await app.UpdateRecurringTemplateAsync(
            new UpdateRecurringTemplateCommand(fijo.Id, "Sueldo", 2_000_000m, 1, EndsOn: null, IsActive: false), _ct)).Value;
        var generados = (await app.GenerateRecurringAsync(new Period(2026, 10), _ct)).Value;

        Assert.Equal((2_000_000m, false), (corregido.EstimatedAmount, corregido.IsActive));
        Assert.Equal(0, generados.Generated);
    }

    [Fact]
    public async Task El_listado_de_movimientos_pagina_y_filtra_por_etiqueta()
    {
        var data = await TestLedger.CreateAsync(fixture, _ct);
        await using var app = App.For(fixture, data);
        foreach (var (dia, monto, etiquetas) in new[] { (10, 8_800m, new[] { data.Lu.Id }), (12, 3_000m, []), (15, 20_000m, []) })
        {
            await app.RegisterExpenseAsync(
                new RegisterExpenseCommand(
                    Guid.NewGuid(), data.MercadoPago.Id, new DateOnly(2025, 9, dia), monto, CurrencyCodes.Ars, data.Helado.Id,
                    TagIds: etiquetas),
                _ct);
        }

        var pagina = (await app.ListTransactionsAsync(
            new ListTransactionsQuery(new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 30), PageSize: 2), _ct)).Value;
        var conLu = (await app.ListTransactionsAsync(
            new ListTransactionsQuery(new DateOnly(2025, 9, 1), new DateOnly(2025, 9, 30), TagId: data.Lu.Id), _ct)).Value;

        Assert.Equal(3, pagina.Total);
        Assert.Equal(new[] { new DateOnly(2025, 9, 15), new DateOnly(2025, 9, 12) }, pagina.Items.Select(item => item.Date));
        Assert.Equal("Helado", pagina.Items[0].CategoryName);
        var helado = Assert.Single(conLu.Items);
        Assert.Equal((-8_800m, data.Lu.Id), (helado.Amount, Assert.Single(helado.TagIds)));
    }

    /// <summary>Un libro creado como en la configuración inicial, con su App apuntando a él.</summary>
    private async Task<(App App, Guid LedgerId)> LibroNuevoAsync()
    {
        var ledgerId = Guid.NewGuid();
        var ownerId = await TestUsers.CreateAsync(fixture, _ct);
        var app = App.For(fixture, ledgerId);
        _ = (await app.CreateLedgerAsync(new CreateLedgerCommand(ledgerId, ownerId, "Mis finanzas"), _ct)).Value;
        return (app, ledgerId);
    }

    private async Task<Guid> CrearBancoAsync(App app) =>
        (await app.CreateAccountAsync(
            new CreateAccountCommand(Guid.NewGuid(), "Banco Supervielle", AccountKind.Bank, CurrencyCodes.Ars), _ct)).Value.Value.Id;
}
