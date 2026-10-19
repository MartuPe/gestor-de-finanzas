using Finanzas.Application.Transactions;

namespace Finanzas.Application.Transfers.RegisterTransfer;

/// <param name="ImpliedRate">R2: la cotización real de la operación, en moneda base por unidad de la otra. Null si es la misma moneda.</param>
public sealed record TransferResponse(TransactionResponse Out, TransactionResponse In, decimal? ImpliedRate);
