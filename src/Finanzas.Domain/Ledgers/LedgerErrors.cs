using Finanzas.Domain.Common;

namespace Finanzas.Domain.Ledgers;

public static class LedgerErrors
{
    public static readonly Error InvalidName =
        Error.Validation("ledger.invalid_name", $"El libro necesita un nombre de hasta {Ledger.NameMaxLength} caracteres.");

    public static readonly Error InvalidCurrency =
        Error.Validation("ledger.invalid_currency", "La moneda base tiene que ser un código ISO 4217, como ARS.");

    public static readonly Error InvalidRateSource =
        Error.Validation("ledger.invalid_rate_source", "La casa de cotización no existe (oficial, blue, bolsa, …).");

    public static readonly Error InvalidTimeZone =
        Error.Validation("ledger.invalid_time_zone", "La zona horaria no existe.");

    public static readonly Error SingleOwner =
        Error.Rule("ledger.single_owner", "Un libro tiene un solo dueño.");

    public static readonly Error AlreadyMember =
        Error.Conflict("ledger.already_member", "La persona ya es miembro del libro.");
}
