namespace Finanzas.Application.Transactions.ConfirmPlanned;

/// <summary>
/// R7: confirma un previsto. Sin monto ni fecha, se confirma tal cual; con monto, se corrige
/// (por ejemplo, la luz vino más cara). Si es una transferencia, se confirman las dos patas.
/// </summary>
/// <param name="Amount">En positivo y en la moneda del previsto.</param>
public sealed record ConfirmPlannedCommand(Guid TransactionId, decimal? Amount = null, DateOnly? Date = null);
