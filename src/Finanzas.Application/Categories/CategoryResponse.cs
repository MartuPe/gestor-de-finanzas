using Finanzas.Domain.Categories;

namespace Finanzas.Application.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    CategoryKind Kind,
    Guid? ParentId,
    string? Color,
    string? Icon,
    bool IsSystem,
    int SortOrder)
{
    public static CategoryResponse From(Category category) =>
        new(category.Id, category.Name, category.Kind, category.ParentId, category.Color, category.Icon, category.IsSystem, category.SortOrder);
}
