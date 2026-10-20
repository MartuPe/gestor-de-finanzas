using Finanzas.Domain.Common;

namespace Finanzas.Api.Http;

/// <summary>
/// Valores de la ruta y de la query string con el mismo formato que en el JSON:
/// <c>/months/2026-10</c>, <c>?mode=purchase</c>, <c>?status=planned</c>.
/// </summary>
internal static class QueryValues
{
    /// <returns>El valor por defecto si no viene; null si viene con un valor que no existe.</returns>
    public static TEnum? Enum<TEnum>(string? value, TEnum? fallback = null)
        where TEnum : struct, System.Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return System.Enum.TryParse<TEnum>(value.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true, out var parsed)
            && System.Enum.IsDefined(parsed)
                ? parsed
                : null;
    }

    public static Period? Period(string? value) =>
        Domain.Common.Period.TryParse(value, out var period) ? period : null;

    /// <summary>Un 400 con el mismo formato que los errores de validación de los casos de uso.</summary>
    public static IResult Invalid(string parameter, string? value) =>
        TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { [parameter] = [$"'{value}' no es un valor válido."] },
            title: "Hay datos inválidos.",
            extensions: new Dictionary<string, object?> { ["code"] = "request.invalid" });
}
