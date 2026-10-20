using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(
    ICurrentUser user,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<UpdateCategoryCommand> validator)
{
    public async Task<Result<CategoryResponse>> HandleAsync(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var category = await categories.GetOwnedAsync(command.Id, user, cancellationToken);
        if (category.IsFailure)
        {
            return category.Error;
        }

        var siblings = await categories.ListAsync(user.LedgerId, cancellationToken);
        if (siblings.Any(other => other.Id != command.Id
            && other.ParentId == category.Value.ParentId
            && string.Equals(other.Name, command.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationErrors.CategoryNameTaken;
        }

        var result = category.Value.Rename(command.Name);
        if (result.IsSuccess)
        {
            result = category.Value.ChangeAppearance(command.Color, command.Icon);
        }

        if (result.IsFailure)
        {
            return result.Error;
        }

        category.Value.ChangeSortOrder(command.SortOrder);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error : CategoryResponse.From(category.Value);
    }
}
