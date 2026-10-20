using Finanzas.Domain.Categories;
using FluentValidation;

namespace Finanzas.Application.Categories.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.ParentId).NotEqual(Guid.Empty);
    }
}
