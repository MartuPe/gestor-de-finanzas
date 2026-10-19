namespace Finanzas.Domain.Statements;

/// <summary>
/// Lo que se guarda en la base. "Cerrado" no se guarda: se deriva de la fecha de cierre
/// (ver <see cref="StatementEffectiveStatus"/>).
/// </summary>
public enum StatementStatus
{
    Open,
    Paid,
}
