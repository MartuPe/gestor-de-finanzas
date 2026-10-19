namespace Finanzas.Domain.Common;

/// <summary>Cómo se traduce el error a HTTP (ver 02-backend §8).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule,
}

/// <summary>Un error esperado. El código no cambia, para que el front lo pueda traducir.</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Rule(string code, string message) => new(code, message, ErrorType.BusinessRule);
}
