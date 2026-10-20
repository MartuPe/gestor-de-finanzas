using Finanzas.Domain.Transactions;

namespace Finanzas.Application.Transactions.ListTransactions;

/// <summary>Movimientos entre dos fechas, del más nuevo al más viejo, con filtros opcionales y paginado.</summary>
/// <param name="Page">Empieza en 1.</param>
public sealed record ListTransactionsQuery(
    DateOnly From,
    DateOnly To,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    Guid? TagId = null,
    TransactionStatus? Status = null,
    int Page = 1,
    int PageSize = 50);
