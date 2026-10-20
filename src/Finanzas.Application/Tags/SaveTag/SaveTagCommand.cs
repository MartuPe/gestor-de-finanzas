namespace Finanzas.Application.Tags.SaveTag;

/// <summary>Crea la etiqueta, o la actualiza si el Id ya existe en el libro.</summary>
public sealed record SaveTagCommand(Guid Id, string Name, string? Color = null);
