namespace Finanzas.Application.Transfers.RegisterTransfer;

/// <summary>
/// Mueve plata entre dos cuentas de dinero. Con monedas distintas es una compra o una venta de dólares:
/// por ejemplo, salen ARS 155.500 del banco y entran USD 100.
/// </summary>
/// <param name="OutId">Id de la pata que sale. Lo genera el front y hace idempotente el pedido.</param>
/// <param name="InId">Id de la pata que entra.</param>
public sealed record RegisterTransferCommand(
    Guid OutId,
    Guid InId,
    Guid FromAccountId,
    Guid ToAccountId,
    DateOnly Date,
    decimal SentAmount,
    string SentCurrency,
    decimal ReceivedAmount,
    string ReceivedCurrency,
    string? Description = null);
