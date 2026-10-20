using Finanzas.Domain.Accounts;

namespace Finanzas.Application.Accounts.CreateAccount;

/// <summary>Una cuenta de dinero: banco, efectivo, billetera o ahorro. Las de crédito tienen su propio comando.</summary>
/// <param name="IsLiquid">Si se omite: banco, efectivo y billetera son líquidas; el ahorro no.</param>
public sealed record CreateAccountCommand(
    Guid Id,
    string Name,
    AccountKind Kind,
    string DefaultCurrency,
    bool? IsLiquid = null,
    bool EarnsYield = false,
    string? Color = null,
    string? Icon = null);
