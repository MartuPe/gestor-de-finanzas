using Finanzas.Domain.Common;

namespace Finanzas.Application.Common;

public static class ApplicationErrors
{
    public static readonly Error LedgerNotFound = Error.NotFound("ledger.not_found", "No se encontró el libro.");

    public static readonly Error AccountNotFound = Error.NotFound("account.not_found", "No se encontró la cuenta.");

    public static readonly Error CategoryNotFound = Error.NotFound("category.not_found", "No se encontró la categoría.");

    public static readonly Error TagNotFound = Error.NotFound("tag.not_found", "No se encontró alguna de las etiquetas.");

    public static readonly Error StatementNotFound = Error.NotFound("statement.not_found", "No se encontró el resumen.");

    public static readonly Error TransactionNotFound = Error.NotFound("transaction.not_found", "No se encontró el movimiento.");

    /// <summary>02-backend §4.2: el mismo Id con otros datos.</summary>
    public static readonly Error IdInUse =
        Error.Conflict("request.id_in_use", "Ese Id ya se usó para otro movimiento.");

    /// <summary>02-backend §4.3: otro dispositivo guardó antes.</summary>
    public static readonly Error ConcurrencyConflict =
        Error.Conflict("request.concurrency", "Otro dispositivo modificó estos datos. Recargá y probá de nuevo.");

    public static readonly Error Duplicate =
        Error.Conflict("request.duplicate", "Ya existe un registro igual.");

    public static readonly Error ExchangeRatesUnavailable =
        Error.Unavailable("exchange_rates.unavailable", "No se pudo consultar la cotización del dólar.");
}
