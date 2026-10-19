using Finanzas.Domain.Common;

namespace Finanzas.Domain.Statements;

public static class StatementErrors
{
    public static readonly Error NotCreditAccount =
        Error.Rule("statement.not_credit_account", "Los resúmenes son solo de tarjetas y créditos.");

    public static readonly Error DueOutsidePeriod =
        Error.Validation("statement.due_outside_period", "El vencimiento tiene que caer dentro del mes del resumen.");

    public static readonly Error ClosingRequired =
        Error.Validation("statement.closing_required", "El resumen de una tarjeta necesita fecha de cierre.");

    public static readonly Error ClosingAfterDue =
        Error.Validation("statement.closing_after_due", "El cierre tiene que ser anterior al vencimiento.");

    public static readonly Error HasPayments =
        Error.Rule("statement.has_payments", "El resumen ya tiene pagos: no se pueden cambiar sus fechas ni mover sus cuotas.");

    public static readonly Error NoteTooLong =
        Error.Validation("statement.note_too_long", $"La nota admite hasta {Statement.NoteMaxLength} caracteres.");
}
