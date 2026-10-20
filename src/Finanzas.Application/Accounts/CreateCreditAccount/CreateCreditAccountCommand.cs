using Finanzas.Domain.Accounts;

namespace Finanzas.Application.Accounts.CreateCreditAccount;

/// <summary>Una tarjeta o un crédito en cuotas (como Mercado Crédito).</summary>
/// <param name="UsualClosingDay">Obligatorio en tarjetas; Mercado Crédito no tiene cierre.</param>
public sealed record CreateCreditAccountCommand(
    Guid Id,
    string Name,
    string DefaultCurrency,
    CreditLineType LineType,
    string Issuer,
    string? Network,
    int? UsualClosingDay,
    int UsualDueDay,
    string? Color = null,
    string? Icon = null);
