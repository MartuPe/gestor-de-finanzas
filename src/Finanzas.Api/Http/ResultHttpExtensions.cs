using Finanzas.Application.Common;
using Finanzas.Domain.Common;

namespace Finanzas.Api.Http;

/// <summary>De <see cref="Result{T}"/> a HTTP (02-backend §8). Todos los errores salen como ProblemDetails con <c>code</c>.</summary>
internal static class ResultHttpExtensions
{
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();

    /// <summary>201 si se creó; 200 si el mismo pedido ya se había guardado (el celular reintentó).</summary>
    public static IResult ToCreatedHttp<T>(this Result<Registration<T>> result) =>
        result.IsFailure ? result.Error.ToProblem()
        : result.Value.Created ? TypedResults.Created((string?)null, result.Value.Value)
        : TypedResults.Ok(result.Value.Value);

    /// <summary>
    /// Documenta en OpenAPI el cuerpo de <see cref="ToHttp{T}"/>: como el endpoint devuelve <see cref="IResult"/>,
    /// sin esto el documento no dice qué JSON viene y la PWA no puede generar sus tipos.
    /// </summary>
    public static RouteHandlerBuilder Returns<T>(this RouteHandlerBuilder builder) => builder.Produces<T>();

    /// <summary>Lo mismo para <see cref="ToCreatedHttp{T}"/>: 201 con lo creado, o 200 si era un reintento.</summary>
    public static RouteHandlerBuilder ReturnsCreated<T>(this RouteHandlerBuilder builder) =>
        builder.Produces<T>(StatusCodes.Status201Created).Produces<T>();

    public static IResult ToProblem(this Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        if (error is ValidationError validation)
        {
            var fields = validation.Fields.ToDictionary(field => CamelCase(field.Key), field => field.Value);
            return TypedResults.ValidationProblem(fields, title: error.Message, extensions: extensions);
        }

        return TypedResults.Problem(title: error.Message, statusCode: StatusFor(error.Type), extensions: extensions);
    }

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Los campos se informan como en el JSON: <c>Amount</c> → <c>amount</c>.</summary>
    private static string CamelCase(string name) =>
        name.Length == 0 ? name : string.Concat(char.ToLowerInvariant(name[0]).ToString(), name.AsSpan(1));
}
