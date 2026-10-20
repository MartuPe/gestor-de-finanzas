namespace Finanzas.Api.Endpoints;

// Cuerpos de los pedidos que llevan el Id en la ruta: el resto de los datos viaja en el JSON.
// Los demás endpoints reciben directo el comando de Application.

public sealed record UpdateAccountRequest(
    string Name,
    string? Color,
    string? Icon,
    int SortOrder,
    int? UsualClosingDay = null,
    int? UsualDueDay = null);

/// <param name="TransactionId">Lo genera el front, como en cualquier alta (02-backend §4.2).</param>
public sealed record OpeningBalanceRequest(Guid TransactionId, DateOnly Date, decimal Amount, string Currency);

public sealed record UpdateCategoryRequest(string Name, string? Color, string? Icon, int SortOrder);

public sealed record SaveTagRequest(string Name, string? Color = null);

public sealed record UpdateRecurringTemplateRequest(
    string Description,
    decimal EstimatedAmount,
    int DayOfMonth,
    DateOnly? EndsOn,
    bool IsActive);

/// <param name="Amount">En positivo y en la moneda del previsto. Sin monto, se confirma tal cual.</param>
public sealed record ConfirmRequest(decimal? Amount = null, DateOnly? Date = null);

public sealed record StatementPaymentRequest(
    Guid OutId,
    Guid InId,
    Guid FromAccountId,
    DateOnly Date,
    decimal PaidAmount,
    string PaidCurrency,
    decimal CreditedAmount,
    string CreditedCurrency);
