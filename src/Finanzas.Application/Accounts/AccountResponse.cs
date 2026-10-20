using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Accounts;

/// <param name="Balance">R1: saldo confirmado por moneda. En una cuenta de crédito es la deuda, en negativo.</param>
/// <param name="ProjectedBalance">El saldo sumando además los previstos.</param>
public sealed record AccountResponse(
    Guid Id,
    string Name,
    AccountKind Kind,
    string DefaultCurrency,
    bool IsLiquid,
    bool EarnsYield,
    string? Color,
    string? Icon,
    int SortOrder,
    bool IsArchived,
    CreditLineResponse? CreditLine,
    IReadOnlyDictionary<string, decimal> Balance,
    IReadOnlyDictionary<string, decimal> ProjectedBalance)
{
    public static AccountResponse From(Account account, AccountBalance? balance = null) =>
        new(
            account.Id,
            account.Name,
            account.Kind,
            account.DefaultCurrency,
            account.IsLiquid,
            account.EarnsYield,
            account.Color,
            account.Icon,
            account.SortOrder,
            account.IsArchived,
            account.CreditLine is { } line
                ? new CreditLineResponse(line.LineType, line.Issuer, line.Network, line.UsualClosingDay, line.UsualDueDay)
                : null,
            (balance?.Confirmed ?? MoneyBag.Empty).ToAmounts(),
            (balance?.Projected ?? MoneyBag.Empty).ToAmounts());
}

public sealed record CreditLineResponse(
    CreditLineType LineType,
    string Issuer,
    string? Network,
    int? UsualClosingDay,
    int UsualDueDay);
