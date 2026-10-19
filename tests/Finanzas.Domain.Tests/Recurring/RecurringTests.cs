using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Services;
using Finanzas.Domain.Transactions;
using static Finanzas.Domain.Tests.Fixtures;

namespace Finanzas.Domain.Tests.Recurring;

public class RecurringTemplateTests
{
    [Fact]
    public void El_aguinaldo_se_repite_cada_6_meses()
    {
        var aguinaldo = RecurringTemplate.Create(
            Guid.NewGuid(), "Aguinaldo", RecurringKind.Income, Banco(), null, Ingreso("Aguinaldo"), Money.Ars(500_000),
            RecurringFrequency.Monthly, interval: 6, dayOfMonth: 30, startsOn: Fecha(2026, 6, 1)).Value;

        Assert.False(aguinaldo.OccursIn(Mes(2026, 5)));
        Assert.True(aguinaldo.OccursIn(Mes(2026, 6)));
        Assert.All(Enumerable.Range(7, 5), mes => Assert.False(aguinaldo.OccursIn(Mes(2026, mes))));
        Assert.True(aguinaldo.OccursIn(Mes(2026, 12)));
    }

    [Fact]
    public void Un_fijo_del_dia_31_en_febrero_cae_el_28()
    {
        var fijo = RecurringTemplate.Create(
            Guid.NewGuid(), "Alquiler", RecurringKind.Expense, Banco(), null, Gasto(), Money.Ars(300_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 31, startsOn: Fecha(2026, 1, 1)).Value;

        Assert.Equal(Fecha(2026, 2, 28), fijo.OccurrenceDate(Mes(2026, 2)));
    }

    [Fact]
    public void El_viaje_a_Mendoza_deja_de_generarse_despues_de_la_sexta_cuota()
    {
        // "Viaje a mendoza 1/6 … 6/6": de septiembre de 2025 a febrero de 2026.
        var mendoza = RecurringTemplate.Create(
            Guid.NewGuid(), "Viaje a Mendoza", RecurringKind.Expense, Banco(), null, Gasto("Viajes"), Money.Ars(22_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 5, startsOn: Fecha(2025, 9, 5), endsOn: Fecha(2026, 2, 5)).Value;

        var meses = Enumerable.Range(0, 12).Select(i => Mes(2025, 9).AddMonths(i)).Where(mendoza.OccursIn).ToList();

        Assert.Equal(6, meses.Count);
        Assert.Equal(Mes(2026, 2), meses[^1]);
    }

    [Fact]
    public void Una_transferencia_programada_necesita_destino()
    {
        var result = RecurringTemplate.Create(
            Guid.NewGuid(), "Ahorro mensual", RecurringKind.Transfer, Banco(), null, null, Money.Ars(800_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 5, startsOn: Fecha(2026, 5, 1));

        Assert.Equal(RecurringErrors.TransferNeedsDestination, result.Error);
    }
}

public class RecurringGeneratorTests
{
    private readonly Account _banco = Banco();
    private readonly Account _ahorro = Ahorro();
    private readonly Account _visa = Visa();
    private readonly Category _sueldo = Ingreso("Sueldo");
    private readonly Category _suscripciones = Gasto("Suscripciones");

    [Fact]
    public void Genera_el_sueldo_el_ahorro_y_YouTube_como_previstos()
    {
        var batch = Generate(Templates(), Mes(2026, 10), alreadyGenerated: []);

        Assert.Empty(batch.Skipped);
        Assert.Equal(4, batch.Transactions.Count); // sueldo + las dos patas del ahorro + YouTube
        Assert.All(batch.Transactions, transaction => Assert.Equal(TransactionStatus.Planned, transaction.Status));
        Assert.All(batch.Transactions, transaction => Assert.Equal(Mes(2026, 10), transaction.RecurringPeriod));

        var patas = batch.Transactions.Where(transaction => transaction.Kind == TransactionKind.Transfer).ToList();
        Assert.Equal(2, patas.Count);
        Assert.Single(patas.Select(pata => pata.TransferGroupId).Distinct());

        // YouTube Premium del 15 de octubre en la Visa cae en el resumen que vence en noviembre.
        var youtube = Assert.Single(batch.Transactions, transaction => transaction.AccountId == _visa.Id);
        var resumen = Assert.Single(batch.NewStatements);
        Assert.Equal(Mes(2026, 11), resumen.Period);
        Assert.Equal(resumen.Id, Assert.Single(youtube.Installments).StatementId);
    }

    [Fact]
    public void Generar_dos_veces_el_mismo_mes_no_duplica()
    {
        var templates = Templates();
        var first = Generate(templates, Mes(2026, 10), alreadyGenerated: []);
        var keys = first.Transactions.Select(GeneratedKey.Of).OfType<GeneratedKey>().ToList();

        var second = Generate(templates, Mes(2026, 10), alreadyGenerated: keys);

        Assert.Empty(second.Transactions);
        Assert.Empty(second.NewStatements);
    }

    [Fact]
    public void Si_falta_la_categoria_se_saltea_ese_fijo_y_se_generan_los_demas()
    {
        var templates = Templates();
        var youtube = templates[2];

        var batch = RecurringGenerator.Generate(
            templates,
            Mes(2026, 10),
            new[] { _banco, _ahorro, _visa }.ToDictionary(account => account.Id),
            new[] { _sueldo }.ToDictionary(category => category.Id),
            [],
            new HashSet<GeneratedKey>());

        var skipped = Assert.Single(batch.Skipped);
        Assert.Equal((youtube.Id, RecurringErrors.CategoryNotFound), (skipped.TemplateId, skipped.Error));
        Assert.Equal(3, batch.Transactions.Count);
    }

    private List<RecurringTemplate> Templates() =>
    [
        RecurringTemplate.Create(
            Guid.NewGuid(), "Sueldo", RecurringKind.Income, _banco, null, _sueldo, Money.Ars(1_800_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 1, startsOn: Fecha(2026, 5, 1)).Value,
        RecurringTemplate.Create(
            Guid.NewGuid(), "Ahorro mensual", RecurringKind.Transfer, _banco, _ahorro, null, Money.Ars(800_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 5, startsOn: Fecha(2026, 5, 1)).Value,
        RecurringTemplate.Create(
            Guid.NewGuid(), "YouTube Premium", RecurringKind.Expense, _visa, null, _suscripciones, Money.Ars(10_000),
            RecurringFrequency.Monthly, interval: 1, dayOfMonth: 15, startsOn: Fecha(2025, 11, 1)).Value,
    ];

    private RecurringBatch Generate(
        IEnumerable<RecurringTemplate> templates,
        Period period,
        IEnumerable<GeneratedKey> alreadyGenerated) =>
        RecurringGenerator.Generate(
            templates,
            period,
            new[] { _banco, _ahorro, _visa }.ToDictionary(account => account.Id),
            new[] { _sueldo, _suscripciones }.ToDictionary(category => category.Id),
            [],
            alreadyGenerated.ToHashSet());
}
