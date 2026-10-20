using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Categories.ListCategories;

/// <summary>Las categorías del libro, ordenadas por tipo y orden. Las subcategorías traen su <c>ParentId</c>.</summary>
public sealed class ListCategoriesHandler(ICurrentUser user, ICategoryRepository categories)
{
    public async Task<Result<IReadOnlyList<CategoryResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var all = await categories.ListAsync(user.LedgerId, cancellationToken);
        return Result<IReadOnlyList<CategoryResponse>>.Success(all
            .OrderBy(category => category.Kind)
            .ThenBy(category => category.SortOrder)
            .ThenBy(category => category.Name, StringComparer.CurrentCulture)
            .Select(CategoryResponse.From)
            .ToList());
    }
}
