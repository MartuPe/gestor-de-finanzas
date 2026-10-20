namespace Finanzas.Application.Ledgers.CreateLedger;

/// <summary>
/// Crea el libro de un usuario nuevo, con las categorías iniciales (01-modelo §4.9).
/// Se usa en la configuración inicial, antes de que haya sesión: por eso recibe al dueño explícitamente.
/// </summary>
public sealed record CreateLedgerCommand(Guid LedgerId, Guid OwnerUserId, string Name);
