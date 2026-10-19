namespace Finanzas.Domain.Statements;

/// <summary>El estado que se muestra: el guardado más el cierre derivado de la fecha de hoy.</summary>
public enum StatementEffectiveStatus
{
    Open,
    Closed,
    Paid,
}
