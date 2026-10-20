using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Tags;

namespace Finanzas.Infrastructure.Tests;

/// <summary>Un libro nuevo y guardado, con las cuentas, categorías y etiquetas del Excel.</summary>
internal sealed class TestLedger
{
    private TestLedger(Ledger ledger)
    {
        Ledger = ledger;
        var id = ledger.Id;

        Banco = Account.Create(Guid.NewGuid(), id, "Banco Supervielle", AccountKind.Bank, CurrencyCodes.Ars).Value;
        MercadoPago = Account.Create(Guid.NewGuid(), id, "Mercado Pago", AccountKind.Wallet, CurrencyCodes.Ars).Value;
        Ahorro = Account.Create(Guid.NewGuid(), id, "Ahorro", AccountKind.Savings, CurrencyCodes.Ars, earnsYield: true).Value;
        Dolares = Account.Create(Guid.NewGuid(), id, "Dólares", AccountKind.Cash, CurrencyCodes.Usd).Value;
        Visa = Account.CreateCredit(
            Guid.NewGuid(), id, "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", 28, 5).Value;
        Mastercard = Account.CreateCredit(
            Guid.NewGuid(), id, "Mastercard", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "mastercard", 28, 5).Value;
        Patagonia = Account.CreateCredit(
            Guid.NewGuid(), id, "Patagonia", CurrencyCodes.Ars, CreditLineType.Card, "Banco Patagonia", "visa", 28, 10).Value;
        MercadoCredito = Account.CreateCredit(
            Guid.NewGuid(), id, "Mercado Crédito", CurrencyCodes.Ars, CreditLineType.InstallmentCredit, "Mercado Pago", null, null, 10).Value;

        Regalos = Category.Create(Guid.NewGuid(), id, "Regalos", CategoryKind.Expense).Value;
        Suscripciones = Category.Create(Guid.NewGuid(), id, "Suscripciones", CategoryKind.Expense).Value;
        Helado = Category.Create(Guid.NewGuid(), id, "Helado", CategoryKind.Expense).Value;
        Varios = Category.Create(Guid.NewGuid(), id, "Otro", CategoryKind.Expense).Value;
        Sueldo = Category.Create(Guid.NewGuid(), id, "Sueldo", CategoryKind.Income).Value;

        Lu = Tag.Create(Guid.NewGuid(), id, "Lu").Value;
    }

    public Ledger Ledger { get; }

    public Account Banco { get; }

    public Account MercadoPago { get; }

    public Account Ahorro { get; }

    public Account Dolares { get; }

    /// <summary>Cierra el 28 y vence el 5.</summary>
    public Account Visa { get; }

    /// <summary>Cierra el 28 y vence el 5.</summary>
    public Account Mastercard { get; }

    /// <summary>Cierra el 28 y vence el 10.</summary>
    public Account Patagonia { get; }

    /// <summary>No tiene cierre: vence el 10.</summary>
    public Account MercadoCredito { get; }

    public Category Regalos { get; }

    public Category Suscripciones { get; }

    public Category Helado { get; }

    public Category Varios { get; }

    public Category Sueldo { get; }

    public Tag Lu { get; }

    public static async Task<TestLedger> CreateAsync(SqlServerFixture fixture, CancellationToken cancellationToken)
    {
        var ownerId = Guid.NewGuid();
        var data = new TestLedger(Ledger.Create(Guid.NewGuid(), "Mis finanzas", ownerId).Value);

        await using var db = fixture.CreateContext();
        db.Users.Add(TestUsers.New(ownerId));
        db.Ledgers.Add(data.Ledger);
        db.Accounts.AddRange(
            data.Banco, data.MercadoPago, data.Ahorro, data.Dolares, data.Visa, data.Mastercard, data.Patagonia, data.MercadoCredito);
        db.Categories.AddRange(data.Regalos, data.Suscripciones, data.Helado, data.Varios, data.Sueldo);
        db.Tags.Add(data.Lu);
        await db.SaveChangesAsync(cancellationToken);
        return data;
    }
}
