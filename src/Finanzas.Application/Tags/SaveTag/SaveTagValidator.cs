using Finanzas.Domain.Tags;
using FluentValidation;

namespace Finanzas.Application.Tags.SaveTag;

public sealed class SaveTagValidator : AbstractValidator<SaveTagCommand>
{
    public SaveTagValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Tag.NameMaxLength);
    }
}
