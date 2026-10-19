using Finanzas.Domain.Common;

namespace Finanzas.Domain.Accounts;

public static class AccountErrors
{
    public static readonly Error InvalidName =
        Error.Validation("account.invalid_name", $"La cuenta necesita un nombre de hasta {Account.NameMaxLength} caracteres.");

    public static readonly Error InvalidCurrency =
        Error.Validation("account.invalid_currency", "La moneda tiene que ser un código ISO 4217, como ARS o USD.");

    public static readonly Error UseCreateCredit =
        Error.Rule("account.use_create_credit", "Las cuentas de crédito se crean con los datos de la tarjeta o del crédito.");

    public static readonly Error InvalidColor =
        Error.Validation("account.invalid_color", "El color tiene que tener el formato #RRGGBB.");

    public static readonly Error InvalidIssuer =
        Error.Validation("account.invalid_issuer", $"Indicá el emisor (banco o empresa), hasta {CreditLine.IssuerMaxLength} caracteres.");

    public static readonly Error InvalidDay =
        Error.Validation("account.invalid_day", "El día de cierre y el de vencimiento tienen que estar entre 1 y 31.");

    public static readonly Error CardNeedsClosingDay =
        Error.Validation("account.card_needs_closing_day", "Una tarjeta necesita un día de cierre.");

    public static readonly Error ClosingEqualsDue =
        Error.Validation("account.closing_equals_due", "El cierre y el vencimiento no pueden caer el mismo día.");
}
