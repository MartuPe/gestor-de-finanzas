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

    public static readonly Error RecurringTemplateNotFound =
        Error.NotFound("recurring.not_found", "No se encontró el fijo.");

    public static readonly Error AccountNameTaken =
        Error.Conflict("account.name_taken", "Ya tenés una cuenta con ese nombre.");

    public static readonly Error CategoryNameTaken =
        Error.Conflict("category.name_taken", "Ya existe una categoría con ese nombre en el mismo nivel.");

    public static readonly Error TagNameTaken =
        Error.Conflict("tag.name_taken", "Ya tenés una etiqueta con ese nombre.");

    public static readonly Error OpeningExists =
        Error.Conflict("account.opening_exists", "La cuenta ya tiene saldo inicial en esa moneda. Para corregirlo, conciliá el saldo.");

    public static readonly Error NotCreditAccount =
        Error.Rule("account.not_credit", "Los días de cierre y vencimiento son solo de tarjetas y créditos.");

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
