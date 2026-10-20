namespace Finanzas.Application.Accounts.SetAccountArchived;

/// <summary>Archivar una cuenta la oculta pero conserva su historia; se puede desarchivar.</summary>
public sealed record SetAccountArchivedCommand(Guid Id, bool Archived);
