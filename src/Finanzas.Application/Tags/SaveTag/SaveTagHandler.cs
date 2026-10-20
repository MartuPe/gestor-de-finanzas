using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Finanzas.Domain.Tags;
using FluentValidation;

namespace Finanzas.Application.Tags.SaveTag;

/// <remarks>
/// Una sola operación para crear y editar: una etiqueta es solo nombre y color, así que el front
/// puede crearla al vuelo mientras carga un gasto ("Pinamar") con un Id propio y renombrarla después.
/// </remarks>
public sealed class SaveTagHandler(
    ICurrentUser user,
    ITagRepository tags,
    IUnitOfWork unitOfWork,
    IValidator<SaveTagCommand> validator)
{
    public async Task<Result<Registration<TagResponse>>> HandleAsync(SaveTagCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var all = await tags.ListAsync(user.LedgerId, cancellationToken);
        if (all.Any(tag => tag.Id != command.Id && string.Equals(tag.Name, command.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationErrors.TagNameTaken;
        }

        var existing = await tags.GetAsync(command.Id, cancellationToken);
        if (existing is not null && existing.LedgerId != user.LedgerId)
        {
            return ApplicationErrors.IdInUse;
        }

        Tag tag;
        if (existing is null)
        {
            var created = Tag.Create(command.Id, user.LedgerId, command.Name, command.Color);
            if (created.IsFailure)
            {
                return created.Error;
            }

            tag = created.Value;
            tags.Add(tag);
        }
        else
        {
            var updated = existing.Update(command.Name, command.Color);
            if (updated.IsFailure)
            {
                return updated.Error;
            }

            tag = existing;
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure
            ? saved.Error
            : new Registration<TagResponse>(TagResponse.From(tag), Created: existing is null);
    }
}
