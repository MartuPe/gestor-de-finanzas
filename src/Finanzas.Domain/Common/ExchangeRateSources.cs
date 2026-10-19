namespace Finanzas.Domain.Common;

/// <summary>Las "casas" de cotización del dólar que publica DolarApi.</summary>
public static class ExchangeRateSources
{
    public const string Oficial = "oficial";
    public const string Blue = "blue";
    public const string Bolsa = "bolsa";
    public const string ContadoConLiqui = "contadoconliqui";
    public const string Mayorista = "mayorista";
    public const string Cripto = "cripto";
    public const string Tarjeta = "tarjeta";

    public static IReadOnlyList<string> All { get; } =
        [Oficial, Blue, Bolsa, ContadoConLiqui, Mayorista, Cripto, Tarjeta];

    public static bool IsValid(string? source) => source is not null && All.Contains(source);
}
