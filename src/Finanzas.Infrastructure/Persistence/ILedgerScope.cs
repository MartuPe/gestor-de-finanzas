namespace Finanzas.Infrastructure.Persistence;

/// <summary>
/// El libro del pedido en curso, para el filtro global <c>Ledger</c>. Lo implementa la Api a partir de la sesión.
/// Sin libro (migraciones, jobs, tests, login), el filtro no restringe nada: los casos de uso igual
/// acotan cada consulta al libro del usuario.
/// </summary>
public interface ILedgerScope
{
    Guid? LedgerId { get; }
}
