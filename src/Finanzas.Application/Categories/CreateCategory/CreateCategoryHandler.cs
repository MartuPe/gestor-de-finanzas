using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Categories.CreateCategory;

public sealed class CreateCategoryHandler(
    ICurrentUser user,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<CreateCategoryCommand> validator)
{
    public async Task<Result<Registration<CategoryResponse>>> HandleAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        if (await categories.GetAsync(command.Id, cancellationToken) is { } existing)
        {
            return existing.LedgerId == user.LedgerId
                ? new Registration<CategoryResponse>(CategoryResponse.From(existing), Created: false)
                : ApplicationErrors.IdInUse;
        }

        Category? parent = null;
        if (command.ParentId is Guid parentId)
        {
            var found = await categories.GetOwnedAsync(parentId, user, cancellationToken);
            if (found.IsFailure)
            {
                return found.Error;
            }

            parent = found.Value;
        }

        var all = await categories.ListAsync(user.LedgerId, cancellationToken);
        if (all.Any(category => category.ParentId == command.ParentId
            && string.Equals(category.Name, command.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationErrors.CategoryNameTaken;
        }

        var created = Category.Create(command.Id, user.LedgerId, command.Name, command.Kind, parent, command.Color, command.Icon);
        if (created.IsFailure)
        {
            return created.Error;
        }

        created.Value.ChangeSortOrder(all.Count);
        categories.AddRange([created.Value]);
        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<CategoryResponse>(CategoryResponse.From(created.Value), Created: true);
    }
}
