using Finanzas.Domain.Common;

namespace Finanzas.Domain.Services;

public static class InstallmentErrors
{
    public static readonly Error NotCreditAccount =
        Error.Rule("installments.not_credit_account", "Solo las cuentas de crédito tienen cuotas.");

    public static readonly Error NotCreditPurchaseOfAccount =
        Error.Rule("installments.not_credit_purchase", "El movimiento no es una compra en cuotas de esa cuenta.");

    public static readonly Error InvalidCount =
        Error.Validation("installments.invalid_count", $"La cantidad de cuotas tiene que estar entre 1 y {InstallmentScheduler.MaxInstallments}.");

    public static readonly Error InvalidFirstNumber =
        Error.Validation("installments.invalid_first_number", "La primera cuota tiene que estar entre 1 y el total de cuotas.");

    public static readonly Error AmountRequired =
        Error.Validation("installments.amount_zero", "El monto no puede ser cero.");

    public static readonly Error RefundInOnePayment =
        Error.Rule("installments.refund_single_payment", "Una devolución se carga en un solo pago.");
}
