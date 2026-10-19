namespace Finanzas.Domain.Common;

/// <summary>Códigos ISO 4217 de las monedas que maneja la app.</summary>
public static class CurrencyCodes
{
    public const string Ars = "ARS";
    public const string Usd = "USD";

    public static bool IsValid(string? code) =>
        code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);

    public static void EnsureValid(string? code)
    {
        if (!IsValid(code))
        {
            throw new ArgumentException($"'{code}' no es un código de moneda ISO 4217.", nameof(code));
        }
    }
}
