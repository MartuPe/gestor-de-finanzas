using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.Transactions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Transfers.RegisterTransfer;

public sealed class RegisterTransferHandler(
    ICurrentUser user,
    ILedgerRepository ledgers,
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<RegisterTransferCommand> validator)
{
    public async Task<Result<Registration<TransferResponse>>> HandleAsync(
        RegisterTransferCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var ledger = await ledgers.CurrentLedgerAsync(user, cancellationToken);
        if (ledger.IsFailure)
        {
            return ledger.Error;
        }

        var sent = new Money(command.SentAmount, command.SentCurrency);
        var received = new Money(command.ReceivedAmount, command.ReceivedCurrency);
        var previous = await Idempotency.CheckAsync(
            transactions,
            command.OutId,
            user,
            existing => existing.Kind == TransactionKind.Transfer
                && existing.AccountId == command.FromAccountId
                && existing.Date == command.Date
                && existing.Money == sent.Negate(),
            cancellationToken);
        if (previous is not null)
        {
            if (previous.IsFailure)
            {
                return previous.Error;
            }

            var legs = await transactions.GetTransferLegsAsync(previous.Value.TransferGroupId!.Value, cancellationToken);
            var replay = new TransferLegs(previous.Value, legs.Single(leg => leg.Id != previous.Value.Id));
            return new Registration<TransferResponse>(Respond(replay, ledger.Value.BaseCurrency), Created: false);
        }

        var from = await accounts.GetOwnedAsync(command.FromAccountId, user, cancellationToken);
        if (from.IsFailure)
        {
            return from.Error;
        }

        var to = await accounts.GetOwnedAsync(command.ToAccountId, user, cancellationToken);
        if (to.IsFailure)
        {
            return to.Error;
        }

        var transfer = TransferFactory.CreateTransfer(
            command.OutId, command.InId, from.Value, to.Value, command.Date, sent, received, command.Description);
        if (transfer.IsFailure)
        {
            return transfer.Error;
        }

        transactions.AddRange([transfer.Value.Out, transfer.Value.In]);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<TransferResponse>(Respond(transfer.Value, ledger.Value.BaseCurrency), Created: true);
    }

    private static TransferResponse Respond(TransferLegs legs, string baseCurrency) =>
        new(TransactionResponse.From(legs.Out), TransactionResponse.From(legs.In), legs.ImpliedRate(baseCurrency));
}
