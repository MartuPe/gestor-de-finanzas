using Finanzas.Domain.Tags;

namespace Finanzas.Application.Tags;

public sealed record TagResponse(Guid Id, string Name, string? Color)
{
    public static TagResponse From(Tag tag) => new(tag.Id, tag.Name, tag.Color);
}
