namespace Finanzas.Application.Incomes.RegisterIncome;

/// <summary>Un ingreso: sueldo, aguinaldo, un cobro. <paramref name="Amount"/> va en positivo.</summary>
public sealed record RegisterIncomeCommand(
    Guid Id,
    Guid AccountId,
    DateOnly Date,
    decimal Amount,
    string Currency,
    Guid CategoryId,
    string? Description = null,
    IReadOnlyList<Guid>? TagIds = null);
