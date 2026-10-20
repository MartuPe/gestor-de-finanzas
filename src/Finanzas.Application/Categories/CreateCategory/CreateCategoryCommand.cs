using Finanzas.Domain.Categories;

namespace Finanzas.Application.Categories.CreateCategory;

/// <param name="ParentId">Para una subcategoría: hay como máximo dos niveles.</param>
public sealed record CreateCategoryCommand(
    Guid Id,
    string Name,
    CategoryKind Kind,
    Guid? ParentId = null,
    string? Color = null,
    string? Icon = null);
