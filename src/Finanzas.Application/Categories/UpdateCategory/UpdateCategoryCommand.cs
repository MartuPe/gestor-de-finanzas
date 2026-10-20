namespace Finanzas.Application.Categories.UpdateCategory;

/// <summary>Cambia nombre, apariencia y orden. El tipo y la categoría padre no cambian.</summary>
public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Color, string? Icon, int SortOrder);
