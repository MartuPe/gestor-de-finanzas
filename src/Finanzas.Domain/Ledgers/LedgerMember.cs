namespace Finanzas.Domain.Ledgers;

public sealed class LedgerMember
{
    private LedgerMember()
    {
    }

    internal LedgerMember(Guid ledgerId, Guid userId, LedgerRole role)
    {
        LedgerId = ledgerId;
        UserId = userId;
        Role = role;
    }

    public Guid LedgerId { get; private set; }

    public Guid UserId { get; private set; }

    public LedgerRole Role { get; private set; }
}
