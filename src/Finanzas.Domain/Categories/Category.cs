using Finanzas.Domain.Common;

namespace Finanzas.Domain.Categories;

/// <summary>En qué se gasta o de dónde viene un ingreso. Hay como máximo dos niveles.</summary>
public sealed class Category : Entity, IAggregateRoot
{
    public const int NameMaxLength = 60;

    private Category()
    {
    }

    private Category(
        Guid id,
        Guid ledgerId,
        Guid? parentId,
        string name,
        CategoryKind kind,
        string? color,
        string? icon,
        bool isSystem)
        : base(id)
    {
        LedgerId = ledgerId;
        ParentId = parentId;
        Name = name;
        Kind = kind;
        Color = color;
        Icon = icon;
        IsSystem = isSystem;
    }

    public Guid LedgerId { get; private set; }

    public Guid? ParentId { get; private set; }

    public string Name { get; private set; } = null!;

    public CategoryKind Kind { get; private set; }

    public string? Color { get; private set; }

    public string? Icon { get; private set; }

    /// <summary>No se puede borrar, como "Ajuste" o "Rendimientos".</summary>
    public bool IsSystem { get; private set; }

    public int SortOrder { get; private set; }

    public static Result<Category> Create(
        Guid id,
        Guid ledgerId,
        string name,
        CategoryKind kind,
        Category? parent = null,
        string? color = null,
        string? icon = null) =>
        CreateCore(id, ledgerId, name, kind, parent, color, icon, isSystem: false);

    public static Result<Category> CreateSystem(Guid id, Guid ledgerId, string name, CategoryKind kind) =>
        CreateCore(id, ledgerId, name, kind, parent: null, color: null, icon: null, isSystem: true);

    public Result Rename(string name)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return CategoryErrors.InvalidName;
        }

        Name = cleanName;
        return Result.Success();
    }

    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;

    private static Result<Category> CreateCore(
        Guid id,
        Guid ledgerId,
        string name,
        CategoryKind kind,
        Category? parent,
        string? color,
        string? icon,
        bool isSystem)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return CategoryErrors.InvalidName;
        }

        if (color is not null && !Text.IsHexColor(color))
        {
            return CategoryErrors.InvalidColor;
        }

        if (parent is not null)
        {
            if (parent.LedgerId != ledgerId)
            {
                return CategoryErrors.ParentFromAnotherLedger;
            }

            if (parent.Kind != kind)
            {
                return CategoryErrors.ParentKindMismatch;
            }

            if (parent.ParentId is not null)
            {
                return CategoryErrors.TooDeep;
            }
        }

        return new Category(id, ledgerId, parent?.Id, cleanName, kind, color, Text.Clean(icon), isSystem);
    }
}
