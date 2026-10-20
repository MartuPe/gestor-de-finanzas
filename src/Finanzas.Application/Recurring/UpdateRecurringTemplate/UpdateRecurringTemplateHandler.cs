using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using FluentValidation;

namespace Finanzas.Application.Recurring.UpdateRecurringTemplate;

public sealed class UpdateRecurringTemplateHandler(
    ICurrentUser user,
    IRecurringTemplateRepository templates,
    IUnitOfWork unitOfWork,
    IValidator<UpdateRecurringTemplateCommand> validator)
{
    public async Task<Result<RecurringTemplateResponse>> HandleAsync(
        UpdateRecurringTemplateCommand command,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var template = await templates.GetAsync(command.Id, cancellationToken);
        if (template is null || template.LedgerId != user.LedgerId)
        {
            return ApplicationErrors.RecurringTemplateNotFound;
        }

        var updated = template.Update(command.Description, command.EstimatedAmount, command.DayOfMonth, command.EndsOn);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        if (command.IsActive)
        {
            template.Activate();
        }
        else
        {
            template.Deactivate();
        }

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        return saved.IsFailure ? saved.Error : RecurringTemplateResponse.From(template);
    }
}
