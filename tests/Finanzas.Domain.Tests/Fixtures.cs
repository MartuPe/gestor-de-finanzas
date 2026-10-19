using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Tags;

namespace Finanzas.Domain.Tests;

/// <summary>Cuentas, categorías y fechas como las del Excel.</summary>
internal static class Fixtures
{
    public static readonly Guid LedgerId = Guid.NewGuid();

    public static Account Banco() =>
        Account.Create(Guid.NewGuid(), LedgerId, "Banco Supervielle", AccountKind.Bank, CurrencyCodes.Ars).Value;

    public static Account MercadoPago() =>
        Account.Create(Guid.NewGuid(), LedgerId, "Mercado Pago", AccountKind.Wallet, CurrencyCodes.Ars).Value;

    public static Account Ahorro() =>
        Account.Create(Guid.NewGuid(), LedgerId, "Ahorro", AccountKind.Savings, CurrencyCodes.Ars, earnsYield: true).Value;

    public static Account Dolares() =>
        Account.Create(Guid.NewGuid(), LedgerId, "Dólares", AccountKind.Cash, CurrencyCodes.Usd).Value;

    /// <summary>Cierra el 28 y vence el 5.</summary>
    public static Account Visa() =>
        Account.CreateCredit(Guid.NewGuid(), LedgerId, "Visa", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "visa", 28, 5).Value;

    /// <summary>Cierra el 28 y vence el 5.</summary>
    public static Account Mastercard() =>
        Account.CreateCredit(Guid.NewGuid(), LedgerId, "Mastercard", CurrencyCodes.Ars, CreditLineType.Card, "Supervielle", "mastercard", 28, 5).Value;

    /// <summary>Cierra el 28 y vence el 10.</summary>
    public static Account Patagonia() =>
        Account.CreateCredit(Guid.NewGuid(), LedgerId, "Patagonia", CurrencyCodes.Ars, CreditLineType.Card, "Banco Patagonia", "visa", 28, 10).Value;

    /// <summary>No tiene cierre: vence el 10.</summary>
    public static Account MercadoCredito() =>
        Account.CreateCredit(Guid.NewGuid(), LedgerId, "Mercado Crédito", CurrencyCodes.Ars, CreditLineType.InstallmentCredit, "Mercado Pago", null, null, 10).Value;

    public static Category Gasto(string name = "Otro") =>
        Category.Create(Guid.NewGuid(), LedgerId, name, CategoryKind.Expense).Value;

    public static Category Ingreso(string name = "Sueldo") =>
        Category.Create(Guid.NewGuid(), LedgerId, name, CategoryKind.Income).Value;

    public static Category Ajuste() =>
        Category.CreateSystem(Guid.NewGuid(), LedgerId, "Ajuste", CategoryKind.Expense).Value;

    public static Tag Etiqueta(string name) => Tag.Create(Guid.NewGuid(), LedgerId, name).Value;

    public static DateOnly Fecha(int year, int month, int day) => new(year, month, day);

    public static Period Mes(int year, int month) => new(year, month);
}
