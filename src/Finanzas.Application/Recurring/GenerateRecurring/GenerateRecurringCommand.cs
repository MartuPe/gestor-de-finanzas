using Finanzas.Domain.Common;

namespace Finanzas.Application.Recurring.GenerateRecurring;

/// <summary>
/// R7: genera los fijos previstos de un mes. Es idempotente: se puede llamar cada vez que se abre
/// un mes, y lo que ya se generó (aunque después se haya borrado) no se repite.
/// </summary>
public sealed record GenerateRecurringCommand(Period Period);
