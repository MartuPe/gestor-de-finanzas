using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;

namespace Finanzas.Application.Common;

/// <summary>
/// 02-backend §4.2: el Id lo genera el front. Si el mismo pedido llega dos veces (el celular reintentó),
/// se devuelve lo que ya estaba guardado; si el Id llega con otros datos, es un conflicto.
/// </summary>
internal static class Idempotency
{
    /// <returns>
    /// null si el Id está libre. Si no, lo que hay que responder: el movimiento existente o un conflicto.
    /// </returns>
    public static async Task<Result<Transaction>?> CheckAsync(
        ITransactionRepository transactions,
        Guid id,
        ICurrentUser user,
        Func<Transaction, bool> isSameRequest,
        CancellationToken cancellationToken)
    {
        var existing = await transactions.GetAsync(id, cancellationToken);
        if (existing is not null)
        {
            return existing.LedgerId == user.LedgerId && isSameRequest(existing)
                ? Result<Transaction>.Success(existing)
                : Result<Transaction>.Failure(ApplicationErrors.IdInUse);
        }

        return await transactions.IdInUseAsync(id, cancellationToken)
            ? Result<Transaction>.Failure(ApplicationErrors.IdInUse)
            : null;
    }
}
