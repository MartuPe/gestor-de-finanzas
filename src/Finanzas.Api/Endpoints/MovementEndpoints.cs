using Finanzas.Api.Http;
using Finanzas.Application.Abstractions;
using Finanzas.Application.Expenses.RegisterExpense;
using Finanzas.Application.Incomes.RegisterIncome;
using Finanzas.Application.Statements.PayStatement;
using Finanzas.Application.Transactions;
using Finanzas.Application.Transactions.ConfirmPlanned;
using Finanzas.Application.Transactions.GetTransaction;
using Finanzas.Application.Transactions.ListTransactions;
using Finanzas.Application.Transfers.RegisterTransfer;
using Finanzas.Domain.Transactions;

namespace Finanzas.Api.Endpoints;

/// <summary>Gastos, ingresos, transferencias, pagos de resumen y el listado de movimientos.</summary>
internal static class MovementEndpoints
{
    public static void MapMovementEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapPost("/expenses", async (RegisterExpenseCommand command, RegisterExpenseHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .WithTags("Movimientos")
            .ReturnsCreated<TransactionResponse>();
        api.MapPost("/incomes", async (RegisterIncomeCommand command, RegisterIncomeHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .WithTags("Movimientos")
            .ReturnsCreated<TransactionResponse>();
        api.MapPost("/transfers", async (RegisterTransferCommand command, RegisterTransferHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(command, cancellationToken)).ToCreatedHttp())
            .WithTags("Movimientos")
            .ReturnsCreated<TransferResponse>();

        var transactions = api.MapGroup("/transactions").WithTags("Movimientos");
        transactions.MapGet("/", async (
                DateOnly from,
                DateOnly to,
                Guid? accountId,
                Guid? categoryId,
                Guid? tagId,
                string? status,
                int? page,
                int? pageSize,
                ListTransactionsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var parsedStatus = QueryValues.Enum<TransactionStatus>(status);
                if (status is not null && parsedStatus is null)
                {
                    return QueryValues.Invalid(nameof(status), status);
                }

                var query = new ListTransactionsQuery(from, to, accountId, categoryId, tagId, parsedStatus, page ?? 1, pageSize ?? 50);
                return (await handler.HandleAsync(query, cancellationToken)).ToHttp();
            })
            .Returns<TransactionPage>();
        transactions.MapGet("/{id:guid}", async (Guid id, GetTransactionHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, cancellationToken)).ToHttp())
            .Returns<TransactionResponse>();
        transactions.MapPost("/{id:guid}/confirm", async (Guid id, ConfirmRequest request, ConfirmPlannedHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(new ConfirmPlannedCommand(id, request.Amount, request.Date), cancellationToken)).ToHttp())
            .Returns<IReadOnlyList<TransactionResponse>>();

        api.MapPost("/statements/{id:guid}/payments", async (
                Guid id,
                StatementPaymentRequest request,
                PayStatementHandler handler,
                CancellationToken cancellationToken) =>
                (await handler.HandleAsync(
                    new PayStatementCommand(
                        request.OutId,
                        request.InId,
                        id,
                        request.FromAccountId,
                        request.Date,
                        request.PaidAmount,
                        request.PaidCurrency,
                        request.CreditedAmount,
                        request.CreditedCurrency),
                    cancellationToken)).ToCreatedHttp())
            .WithTags("Resúmenes")
            .ReturnsCreated<PayStatementResponse>();
    }
}
