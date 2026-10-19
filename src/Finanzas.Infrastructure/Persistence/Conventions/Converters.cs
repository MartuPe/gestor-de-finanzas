using Finanzas.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Finanzas.Infrastructure.Persistence.Conventions;

internal sealed class SnakeCaseEnumConverter<TEnum>()
    : ValueConverter<TEnum, string>(
        value => EnumNames<TEnum>.ToDatabase(value),
        value => EnumNames<TEnum>.FromDatabase(value))
    where TEnum : struct, Enum;

/// <summary>Un período se guarda como <c>date</c>: el primer día del mes.</summary>
internal sealed class PeriodConverter()
    : ValueConverter<Period, DateOnly>(
        period => period.FirstDay,
        date => Period.FromDate(date));

/// <summary>Los instantes se guardan en <c>datetime2</c> y siempre en UTC.</summary>
internal sealed class UtcDateTimeOffsetConverter()
    : ValueConverter<DateTimeOffset, DateTime>(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
