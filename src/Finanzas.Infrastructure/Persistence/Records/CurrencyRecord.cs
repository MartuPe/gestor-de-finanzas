namespace Finanzas.Infrastructure.Persistence.Records;

/// <summary>Fila de <c>Currencies</c>: datos de referencia, sin comportamiento de dominio.</summary>
public sealed class CurrencyRecord
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Symbol { get; init; }
}
