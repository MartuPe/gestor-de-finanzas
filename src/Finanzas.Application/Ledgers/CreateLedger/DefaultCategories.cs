using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Ledgers.CreateLedger;

/// <summary>
/// Semilla propuesta en 01-modelo §4.9: las categorías del Excel más las que surgen de la hoja Cuotas.
/// Se pueden editar; "Ajuste" y "Rendimientos" son de sistema y no se borran.
/// </summary>
internal static class DefaultCategories
{
    private static readonly string[] Expenses =
    [
        "Comida", "Helado", "Nafta", "Cancha", "Veterinaria", "Supermercado", "Regalos", "Ropa", "Deporte",
        "Viajes", "Suscripciones", "Belleza", "Trámites", "Transporte", "Impuestos y comisiones", "Otro",
    ];

    private static readonly string[] Incomes = ["Sueldo", "Aguinaldo", "Otros ingresos"];

    public static IReadOnlyList<Category> For(Guid ledgerId)
    {
        var categories = new List<Category>();
        categories.AddRange(Expenses.Select((name, index) => Create(ledgerId, name, CategoryKind.Expense, index)));
        categories.AddRange(Incomes.Select((name, index) => Create(ledgerId, name, CategoryKind.Income, index)));
        categories.Add(Category.CreateSystem(SequentialGuid.New(), ledgerId, "Rendimientos", CategoryKind.Income).Value);
        categories.Add(Category.CreateSystem(SequentialGuid.New(), ledgerId, "Ajuste", CategoryKind.Expense).Value);
        return categories;
    }

    private static Category Create(Guid ledgerId, string name, CategoryKind kind, int sortOrder)
    {
        var category = Category.Create(SequentialGuid.New(), ledgerId, name, kind).Value;
        category.ChangeSortOrder(sortOrder);
        return category;
    }
}
