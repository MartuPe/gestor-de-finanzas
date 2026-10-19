using Finanzas.Domain.Common;

namespace Finanzas.Domain.Transactions;

public static class TransferErrors
{
    public static readonly Error SameAccount =
        Error.Validation("transfer.same_account", "El origen y el destino tienen que ser cuentas distintas.");

    public static readonly Error DifferentLedger =
        Error.Rule("transfer.different_ledger", "Las dos cuentas tienen que ser del mismo libro.");

    public static readonly Error AmountMustBePositive =
        Error.Validation("transfer.amount_not_positive", "Los montos de una transferencia van en positivo.");

    public static readonly Error SameCurrencyDifferentAmounts =
        Error.Validation("transfer.same_currency_different_amounts", "En la misma moneda, lo que sale y lo que entra tiene que ser igual.");

    public static readonly Error CreditAccountsUseCardPayment =
        Error.Rule("transfer.credit_use_card_payment", "Para pasar plata a una tarjeta o crédito, registrá el pago de su resumen.");

    public static readonly Error NotCreditAccount =
        Error.Rule("transfer.not_credit_account", "Solo se pagan resúmenes de tarjetas y créditos.");

    public static readonly Error PayFromCreditAccount =
        Error.Rule("transfer.pay_from_credit", "Un resumen se paga desde una cuenta de dinero, no desde otra tarjeta.");

    public static readonly Error StatementOfAnotherAccount =
        Error.Rule("transfer.statement_other_account", "El resumen es de otra tarjeta o crédito.");

    public static readonly Error NotSameTransfer =
        Error.Rule("transfer.not_same_transfer", "Los movimientos no son las dos patas de una misma transferencia.");
}
