namespace Finanzas.Domain.Transactions;

public sealed class TransactionTag
{
    private TransactionTag()
    {
    }

    internal TransactionTag(Guid transactionId, Guid tagId)
    {
        TransactionId = transactionId;
        TagId = tagId;
    }

    public Guid TransactionId { get; private set; }

    public Guid TagId { get; private set; }
}
