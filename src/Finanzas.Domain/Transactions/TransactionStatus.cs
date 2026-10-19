namespace Finanzas.Domain.Transactions;

public enum TransactionStatus
{
    /// <summary>Previsto: ya resta del disponible, pero todavía no pasó.</summary>
    Planned,

    Confirmed,
}
