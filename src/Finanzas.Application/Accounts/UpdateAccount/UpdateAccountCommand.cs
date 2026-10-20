namespace Finanzas.Application.Accounts.UpdateAccount;

/// <param name="UsualDueDay">Solo para tarjetas y créditos: si viene, se actualizan los días habituales.</param>
public sealed record UpdateAccountCommand(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    int SortOrder,
    int? UsualClosingDay = null,
    int? UsualDueDay = null);
