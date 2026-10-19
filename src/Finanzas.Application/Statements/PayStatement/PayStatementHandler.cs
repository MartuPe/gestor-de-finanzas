using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Application.Transactions;
using Finanzas.Domain.Common;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Statements.PayStatement;

public sealed class PayStatementHandler(
    ICurrentUser user,
    IAccountRepository accounts,
    IStatementRepository statements,
    IStatementReader statementReader,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    IValidator<PayStatementCommand> validator)
{
    public async Task<Result<Registration<PayStatementResponse>>> HandleAsync(
        PayStatementCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var paid = new Money(command.PaidAmount, command.PaidCurrency);
        var credited = new Money(command.CreditedAmount, command.CreditedCurrency);
        var previous = await Idempotency.CheckAsync(
            transactions,
            command.OutId,
            user,
            existing => existing.Kind == TransactionKind.CardPayment
                && existing.PaidStatementId == command.StatementId
                && existing.AccountId == command.FromAccountId
                && existing.Money == paid.Negate(),
            cancellationToken);
        if (previous is not null)
        {
            return previous.IsFailure ? previous.Error : await ReplayAsync(previous.Value, cancellationToken);
        }

        var statement = await statements.GetAsync(command.StatementId, cancellationToken);
        if (statement is null)
        {
            return ApplicationErrors.StatementNotFound;
        }

        var creditAccount = await accounts.GetOwnedAsync(statement.CreditAccountId, user, cancellationToken);
        if (creditAccount.IsFailure)
        {
            return ApplicationErrors.StatementNotFound;
        }

        var from = await accounts.GetOwnedAsync(command.FromAccountId, user, cancellationToken);
        if (from.IsFailure)
        {
            return from.Error;
        }

        var payment = TransferFactory.CreateCardPayment(
            command.OutId, command.InId, from.Value, creditAccount.Value, statement, command.Date, paid, credited);
        if (payment.IsFailure)
        {
            return payment.Error;
        }

        // R6: el estado se recalcula con lo cargado y todo lo pagado, incluido este pago.
        var balance = await statementReader.GetBalanceAsync(statement.Id, cancellationToken);
        var charges = balance?.Charges ?? MoneyBag.Empty;
        var payments = (balance?.Payments ?? MoneyBag.Empty) + MoneyBag.Of(payment.Value.In.Money);
        statement.Settle(charges, payments);

        transactions.AddRange([payment.Value.Out, payment.Value.In]);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        var response = new PayStatementResponse(
            TransactionResponse.From(payment.Value.Out),
            TransactionResponse.From(payment.Value.In),
            statement.Status,
            Statement.Outstanding(charges, payments).ToAmounts());
        return new Registration<PayStatementResponse>(response, Created: true);
    }

    private async Task<Result<Registration<PayStatementResponse>>> ReplayAsync(Transaction outLeg, CancellationToken cancellationToken)
    {
        var legs = await transactions.GetTransferLegsAsync(outLeg.TransferGroupId!.Value, cancellationToken);
        var inLeg = legs.Single(leg => leg.Id != outLeg.Id);
        var balance = await statementReader.GetBalanceAsync(outLeg.PaidStatementId!.Value, cancellationToken);
        if (balance is null)
        {
            return ApplicationErrors.StatementNotFound;
        }

        var response = new PayStatementResponse(
            TransactionResponse.From(outLeg),
            TransactionResponse.From(inLeg),
            balance.Status,
            balance.Outstanding.ToAmounts());
        return new Registration<PayStatementResponse>(response, Created: false);
    }
}
