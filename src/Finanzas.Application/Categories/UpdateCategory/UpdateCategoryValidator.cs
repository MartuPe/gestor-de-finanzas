using Finanzas.Domain.Categories;
using FluentValidation;

namespace Finanzas.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
    }
}
