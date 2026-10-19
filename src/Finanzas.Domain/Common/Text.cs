namespace Finanzas.Domain.Common;

internal static class Text
{
    /// <summary>Recorta los espacios y devuelve null si el texto queda vacío.</summary>
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsHexColor(string? value) =>
        value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(char.IsAsciiHexDigit);
}
