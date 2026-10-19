namespace Finanzas.Infrastructure.Persistence.Records;

/// <summary>
/// Fila de <c>ExchangeRates</c>: cuántos ARS vale 1 unidad de <see cref="Currency"/> en una casa y un día.
/// Son datos globales, no de un libro.
/// </summary>
public sealed class ExchangeRateRecord
{
    public long Id { get; private set; }

    public required string Currency { get; init; }

    public required string Source { get; init; }

    public DateOnly RateDate { get; init; }

    public decimal Buy { get; set; }

    public decimal Sell { get; set; }

    public DateTime FetchedAt { get; set; }
}
