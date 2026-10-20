using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Tags.ListTags;

public sealed class ListTagsHandler(ICurrentUser user, ITagRepository tags)
{
    public async Task<Result<IReadOnlyList<TagResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var all = await tags.ListAsync(user.LedgerId, cancellationToken);
        return Result<IReadOnlyList<TagResponse>>.Success(all
            .OrderBy(tag => tag.Name, StringComparer.CurrentCulture)
            .Select(TagResponse.From)
            .ToList());
    }
}
