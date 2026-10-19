using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Finanzas.Domain.Common;

/// <summary>Un mes calendario (año-mes). Se escribe "2026-10".</summary>
public readonly record struct Period : IComparable<Period>
{
    public Period(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9998);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

    public DateOnly FirstDay => new(Year, Month, 1);

    public DateOnly LastDay => new(Year, Month, DaysInMonth);

    public static Period FromDate(DateOnly date) => new(date.Year, date.Month);

    /// <summary>
    /// El día pedido dentro de este mes. Si el mes es más corto, devuelve el último día:
    /// por ejemplo, el día 31 de febrero es el 28 (o el 29).
    /// </summary>
    public DateOnly Day(int day)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 31);
        return new DateOnly(Year, Month, Math.Min(day, DaysInMonth));
    }

    public Period AddMonths(int months)
    {
        var date = FirstDay.AddMonths(months);
        return new Period(date.Year, date.Month);
    }

    public Period Next() => AddMonths(1);

    public Period Previous() => AddMonths(-1);

    /// <summary>Cuántos meses hay desde este período hasta <paramref name="other"/> (negativo si es anterior).</summary>
    public int MonthsUntil(Period other) => ((other.Year - Year) * 12) + (other.Month - Month);

    public bool Contains(DateOnly date) => date.Year == Year && date.Month == Month;

    public int CompareTo(Period other) => (Year, Month).CompareTo((other.Year, other.Month));

    public static bool operator <(Period left, Period right) => left.CompareTo(right) < 0;

    public static bool operator >(Period left, Period right) => left.CompareTo(right) > 0;

    public static bool operator <=(Period left, Period right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Period left, Period right) => left.CompareTo(right) >= 0;

    public static Period Parse(string value) =>
        TryParse(value, out var period)
            ? period
            : throw new FormatException($"'{value}' no es un período con formato yyyy-MM.");

    public static bool TryParse([NotNullWhen(true)] string? value, out Period period)
    {
        period = default;
        if (value is not { Length: 7 } || value[4] != '-')
        {
            return false;
        }

        if (!int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(value.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || year is < 1 or > 9998
            || month is < 1 or > 12)
        {
            return false;
        }

        period = new Period(year, month);
        return true;
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}");
}
