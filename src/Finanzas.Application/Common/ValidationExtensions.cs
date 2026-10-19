using Finanzas.Domain.Common;
using Finanzas.Domain.Services;
using Finanzas.Domain.Transactions;
using FluentValidation;

namespace Finanzas.Application.Common;

internal static class ValidationExtensions
{
    /// <summary>null si el pedido es válido; si no, el error con el detalle por campo.</summary>
    public static async Task<ValidationError?> ValidateToErrorAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        return result.IsValid ? null : new ValidationError(result.ToDictionary().AsReadOnly());
    }

    /// <summary>Positivo y con hasta 2 decimales: así <see cref="Money"/> nunca recibe un monto inválido.</summary>
    public static IRuleBuilderOptions<T, decimal> IsAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThan(0m)
            .WithMessage("El monto tiene que ser mayor a cero.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .WithMessage("El monto admite hasta 2 decimales.");

    public static IRuleBuilderOptions<T, string> IsCurrencyCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(CurrencyCodes.IsValid)
            .WithMessage("La moneda tiene que ser un código ISO 4217, como ARS o USD.");

    public static IRuleBuilderOptions<T, Period> IsPeriod<T>(this IRuleBuilder<T, Period> rule) =>
        rule.Must(period => period.Year > 0)
            .WithMessage("El período tiene que tener el formato yyyy-MM.");

    public static IRuleBuilderOptions<T, string?> IsDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(Transaction.DescriptionMaxLength);

    public static IRuleBuilderOptions<T, int> IsInstallmentCount<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, InstallmentScheduler.MaxInstallments);
}
