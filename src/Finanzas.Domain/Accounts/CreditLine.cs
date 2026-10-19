using Finanzas.Domain.Common;

namespace Finanzas.Domain.Accounts;

/// <summary>Datos de una cuenta de crédito: tarjeta o crédito en cuotas.</summary>
public sealed class CreditLine
{
    public const int IssuerMaxLength = 60;

    private CreditLine()
    {
    }

    private CreditLine(
        Guid accountId,
        CreditLineType lineType,
        string issuer,
        string? network,
        int? usualClosingDay,
        int usualDueDay)
    {
        AccountId = accountId;
        LineType = lineType;
        Issuer = issuer;
        Network = network;
        UsualClosingDay = usualClosingDay;
        UsualDueDay = usualDueDay;
    }

    public Guid AccountId { get; private set; }

    public CreditLineType LineType { get; private set; }

    public string Issuer { get; private set; } = null!;

    public string? Network { get; private set; }

    /// <summary>Día de cierre habitual. Solo sirve para estimar resúmenes futuros: las fechas reales viven en cada resumen.</summary>
    public int? UsualClosingDay { get; private set; }

    public int UsualDueDay { get; private set; }

    public Result ChangeUsualDays(int? usualClosingDay, int usualDueDay)
    {
        var error = ValidateDays(LineType, usualClosingDay, usualDueDay);
        if (error is not null)
        {
            return error;
        }

        UsualClosingDay = usualClosingDay;
        UsualDueDay = usualDueDay;
        return Result.Success();
    }

    /// <summary>
    /// Fechas propuestas para el resumen que vence en <paramref name="period"/>.
    /// El cierre es el último día de cierre habitual anterior al vencimiento:
    /// una Visa que cierra el 28 y vence el 5 tiene, en octubre, cierre el 28/09 y vencimiento el 05/10.
    /// </summary>
    public (DateOnly? ClosingDate, DateOnly DueDate) EstimateDates(Period period)
    {
        var due = period.Day(UsualDueDay);
        if (UsualClosingDay is not int closingDay)
        {
            return (null, due);
        }

        var closing = period.Day(closingDay);
        if (closing >= due)
        {
            closing = period.Previous().Day(closingDay);
        }

        return (closing, due);
    }

    internal static Result<CreditLine> Create(
        Guid accountId,
        CreditLineType lineType,
        string issuer,
        string? network,
        int? usualClosingDay,
        int usualDueDay)
    {
        var cleanIssuer = Text.Clean(issuer);
        if (cleanIssuer is null || cleanIssuer.Length > IssuerMaxLength)
        {
            return AccountErrors.InvalidIssuer;
        }

        var error = ValidateDays(lineType, usualClosingDay, usualDueDay);
        if (error is not null)
        {
            return error;
        }

        var cleanNetwork = lineType == CreditLineType.Card ? Text.Clean(network) : null;
        return new CreditLine(accountId, lineType, cleanIssuer, cleanNetwork, usualClosingDay, usualDueDay);
    }

    private static Error? ValidateDays(CreditLineType lineType, int? closingDay, int dueDay)
    {
        if (!IsDayOfMonth(dueDay) || (closingDay is int day && !IsDayOfMonth(day)))
        {
            return AccountErrors.InvalidDay;
        }

        if (lineType == CreditLineType.Card && closingDay is null)
        {
            return AccountErrors.CardNeedsClosingDay;
        }

        if (closingDay == dueDay)
        {
            return AccountErrors.ClosingEqualsDue;
        }

        return null;
    }

    private static bool IsDayOfMonth(int day) => day is >= 1 and <= 31;
}
