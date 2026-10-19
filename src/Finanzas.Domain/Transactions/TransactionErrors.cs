using Finanzas.Domain.Common;

namespace Finanzas.Domain.Transactions;

public static class TransactionErrors
{
    public static readonly Error AmountRequired =
        Error.Validation("transaction.amount_zero", "El monto no puede ser cero.");

    public static readonly Error AmountMustBePositive =
        Error.Validation("transaction.amount_not_positive", "El monto tiene que ser mayor a cero.");

    public static readonly Error DescriptionTooLong =
        Error.Validation("transaction.description_too_long", $"La descripción admite hasta {Transaction.DescriptionMaxLength} caracteres.");

    public static readonly Error CurrencyMismatch =
        Error.Validation("transaction.currency_mismatch", "Los montos tienen que estar en la misma moneda.");

    public static readonly Error ArchivedAccount =
        Error.Rule("transaction.account_archived", "La cuenta está archivada.");

    public static readonly Error CategoryFromAnotherLedger =
        Error.Rule("transaction.category_other_ledger", "La categoría es de otro libro.");

    public static readonly Error CategoryKindMismatch =
        Error.Rule("transaction.category_kind", "La categoría no corresponde al tipo de movimiento.");

    public static readonly Error CategoryNotAllowed =
        Error.Rule("transaction.category_not_allowed", "Este tipo de movimiento no lleva categoría.");

    public static readonly Error PlanRequiredForCredit =
        Error.Rule("transaction.plan_required", "Una compra con tarjeta o crédito necesita sus cuotas.");

    public static readonly Error PlanOnlyForCredit =
        Error.Rule("transaction.plan_not_allowed", "Solo las cuentas de crédito tienen cuotas.");

    public static readonly Error PlanForAnotherAccount =
        Error.Rule("transaction.plan_other_account", "Las cuotas se calcularon para otra cuenta.");

    public static readonly Error PlanTotalMismatch =
        Error.Rule("transaction.plan_total", "Las cuotas no suman el monto de la compra.");

    public static readonly Error IncomeOnCredit =
        Error.Rule("transaction.income_on_credit", "No se puede registrar un ingreso en una cuenta de crédito.");

    public static readonly Error AccountDoesNotEarnYield =
        Error.Rule("transaction.account_without_yield", "Esta cuenta no genera rendimientos.");

    public static readonly Error NothingToAdjust =
        Error.Rule("transaction.nothing_to_adjust", "El saldo real coincide con el calculado: no hay nada que ajustar.");

    public static readonly Error AlreadyConfirmed =
        Error.Conflict("transaction.already_confirmed", "El movimiento ya está confirmado.");

    public static readonly Error ConfirmTransferTogether =
        Error.Rule("transaction.confirm_transfer_together", "Las dos patas de una transferencia se confirman juntas.");

    public static readonly Error CannotMoveCreditPurchase =
        Error.Rule("transaction.cannot_move_credit_purchase", "La fecha de una compra con cuotas no se cambia al confirmarla.");

    public static readonly Error UseEditInstallments =
        Error.Rule("transaction.use_edit_installments", "Una compra en varias cuotas se corrige editando sus cuotas.");

    public static readonly Error NotACreditPurchase =
        Error.Rule("transaction.not_credit_purchase", "El movimiento no es una compra en cuotas.");

    public static readonly Error InstallmentCountMismatch =
        Error.Validation("transaction.installment_count", "Hay que indicar un monto por cada cuota.");

    public static readonly Error InvalidInstallmentAmount =
        Error.Validation("transaction.installment_amount", "Cada cuota tiene que tener el signo de la compra y como máximo 2 decimales.");

    public static readonly Error TagFromAnotherLedger =
        Error.Rule("transaction.tag_other_ledger", "La etiqueta es de otro libro.");
}
