namespace Finanzas.Application.Expenses.RegisterExpense;

/// <summary>Un gasto. Si la cuenta es de crédito, se reparte en <see cref="Installments"/> cuotas.</summary>
/// <param name="Id">Lo genera el front: hace idempotente el pedido (02-backend §4.2).</param>
/// <param name="Amount">En positivo.</param>
public sealed record RegisterExpenseCommand(
    Guid Id,
    Guid AccountId,
    DateOnly Date,
    decimal Amount,
    string Currency,
    Guid CategoryId,
    string? Description = null,
    int Installments = 1,
    IReadOnlyList<Guid>? TagIds = null);
