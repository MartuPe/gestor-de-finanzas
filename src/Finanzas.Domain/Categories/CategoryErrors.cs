using Finanzas.Domain.Common;

namespace Finanzas.Domain.Categories;

public static class CategoryErrors
{
    public static readonly Error InvalidName =
        Error.Validation("category.invalid_name", $"La categoría necesita un nombre de hasta {Category.NameMaxLength} caracteres.");

    public static readonly Error InvalidColor =
        Error.Validation("category.invalid_color", "El color tiene que tener el formato #RRGGBB.");

    public static readonly Error ParentFromAnotherLedger =
        Error.Rule("category.parent_other_ledger", "La categoría padre es de otro libro.");

    public static readonly Error ParentKindMismatch =
        Error.Rule("category.parent_kind", "Una subcategoría tiene que ser del mismo tipo (gasto o ingreso) que su padre.");

    public static readonly Error TooDeep =
        Error.Rule("category.too_deep", "Las categorías tienen como máximo dos niveles.");
}
