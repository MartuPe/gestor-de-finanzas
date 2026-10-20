using Finanzas.Application.Abstractions;
using Finanzas.Domain.Common;

namespace Finanzas.Application.Recurring.ListRecurringTemplates;

/// <summary>Los fijos del libro: primero los activos, por día del mes.</summary>
public sealed class ListRecurringTemplatesHandler(ICurrentUser user, IRecurringTemplateRepository templates)
{
    public async Task<Result<IReadOnlyList<RecurringTemplateResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var all = await templates.ListAsync(user.LedgerId, cancellationToken);
        return Result<IReadOnlyList<RecurringTemplateResponse>>.Success(all
            .OrderByDescending(template => template.IsActive)
            .ThenBy(template => template.DayOfMonth)
            .ThenBy(template => template.Description, StringComparer.CurrentCulture)
            .Select(RecurringTemplateResponse.From)
            .ToList());
    }
}
