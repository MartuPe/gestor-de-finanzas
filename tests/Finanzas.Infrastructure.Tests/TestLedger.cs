using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Tags;

namespace Finanzas.Infrastructure.Tests;

/// <summary>Un libro nuevo y guardado, con una cuenta de banco, una Visa, dos categorías y una etiqueta.</summary>
internal sealed record TestLedger(Ledger Ledger, Account Banco, Account Visa, Category Regalos, Category Suscripciones, Tag Lu)
{
    public static async Task<TestLedger> CreateAsync(SqlServerFixture fixture, CancellationToken cancellationToken)
    {
        var ledger = Ledger.Create(Guid.NewGuid(), "Mis finanzas", Guid.NewGuid()).Value;
        var data = new TestLedger(
            ledger,
            Account.Create(Guid.NewGuid(), ledger.Id, "Banco Supervielle", AccountKind.Bank, CurrencyCodes.Ars).Value,
            Account.CreateCredit(
                Guid.NewGuid(), ledger.Id, "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", 28, 5).Value,
            Category.Create(Guid.NewGuid(), ledger.Id, "Regalos", CategoryKind.Expense).Value,
            Category.Create(Guid.NewGuid(), ledger.Id, "Suscripciones", CategoryKind.Expense).Value,
            Tag.Create(Guid.NewGuid(), ledger.Id, "Lu").Value);

        await using var db = fixture.CreateContext();
        db.Ledgers.Add(data.Ledger);
        db.Accounts.AddRange(data.Banco, data.Visa);
        db.Categories.AddRange(data.Regalos, data.Suscripciones);
        db.Tags.Add(data.Lu);
        await db.SaveChangesAsync(cancellationToken);
        return data;
    }
}
