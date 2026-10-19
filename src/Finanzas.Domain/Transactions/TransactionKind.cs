namespace Finanzas.Domain.Transactions;

/// <summary>Tipos de movimiento (01-modelo-de-datos §4.10).</summary>
public enum TransactionKind
{
    /// <summary>Gasto. Monto negativo. En una cuenta de crédito genera cuotas.</summary>
    Expense,

    /// <summary>Devolución o reintegro de un gasto. Monto positivo.</summary>
    Refund,

    /// <summary>Ingreso: sueldo, aguinaldo. Monto positivo.</summary>
    Income,

    /// <summary>Rendimiento de una cuenta que genera intereses. Monto positivo.</summary>
    Yield,

    /// <summary>Una de las dos patas de una transferencia entre cuentas.</summary>
    Transfer,

    /// <summary>Una de las dos patas del pago de un resumen.</summary>
    CardPayment,

    /// <summary>Saldo inicial de una cuenta.</summary>
    Opening,

    /// <summary>Diferencia entre el saldo real y el calculado (R12).</summary>
    Adjustment,
}
