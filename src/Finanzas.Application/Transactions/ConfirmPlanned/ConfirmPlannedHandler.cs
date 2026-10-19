using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Transactions.ConfirmPlanned;

public sealed class ConfirmPlannedHandler(
    ICurrentUser user,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<ConfirmPlannedCommand> validator)
{
    /// <returns>Los movimientos confirmados: uno, o las dos patas de una transferencia.</returns>
    public async Task<Result<IReadOnlyList<TransactionResponse>>> HandleAsync(
        ConfirmPlannedCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var transaction = await transactions.GetAsync(command.TransactionId, cancellationToken);
        if (transaction is null || transaction.LedgerId != user.LedgerId)
        {
            return ApplicationErrors.TransactionNotFound;
        }

        IReadOnlyList<Transaction> confirmed;
        if (transaction.TransferGroupId is Guid groupId)
        {
            var legs = await transactions.GetTransferLegsAsync(groupId, cancellationToken);
            var outLeg = legs.Single(leg => leg.Amount < 0);
            var inLeg = legs.Single(leg => leg.Amount > 0);

            // Un nuevo monto vale para las dos patas solo si están en la misma moneda (como el ahorro mensual).
            Money? sent = command.Amount is decimal amount ? new Money(amount, outLeg.Currency) : null;
            Money? received = sent is Money value && inLeg.Currency == outLeg.Currency ? value : null;

            var result = TransferFactory.Confirm(new TransferLegs(outLeg, inLeg), command.Date, sent, received);
            if (result.IsFailure)
            {
                return result.Error;
            }

            confirmed = [outLeg, inLeg];
        }
        else
        {
            Money? actual = command.Amount is decimal amount ? new Money(amount, transaction.Currency) : null;
            var result = transaction.Confirm(actual, command.Date);
            if (result.IsFailure)
            {
                return result.Error;
            }

            confirmed = [transaction];
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : Result<IReadOnlyList<TransactionResponse>>.Success(confirmed.Select(item => TransactionResponse.From(item)).ToList());
    }
}
