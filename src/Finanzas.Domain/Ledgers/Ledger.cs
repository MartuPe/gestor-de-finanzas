using Finanzas.Domain.Common;

namespace Finanzas.Domain.Ledgers;

/// <summary>El libro: todos los datos de finanzas pertenecen a uno, y los usuarios son sus miembros.</summary>
public sealed class Ledger : Entity, IAggregateRoot
{
    public const string DefaultTimeZone = "America/Argentina/Buenos_Aires";
    public const int NameMaxLength = 100;

    private readonly List<LedgerMember> _members = [];

    private Ledger()
    {
    }

    private Ledger(Guid id, string name, string baseCurrency, string referenceRateSource, string timeZone)
        : base(id)
    {
        Name = name;
        BaseCurrency = baseCurrency;
        ReferenceRateSource = referenceRateSource;
        TimeZone = timeZone;
    }

    public string Name { get; private set; } = null!;

    public string BaseCurrency { get; private set; } = null!;

    /// <summary>Casa de cotización con la que se convierten los totales (R9).</summary>
    public string ReferenceRateSource { get; private set; } = null!;

    /// <summary>Zona horaria IANA que define qué es "hoy" para este libro.</summary>
    public string TimeZone { get; private set; } = null!;

    public IReadOnlyList<LedgerMember> Members => _members;

    public static Result<Ledger> Create(
        Guid id,
        string name,
        Guid ownerUserId,
        string baseCurrency = CurrencyCodes.Ars,
        string referenceRateSource = ExchangeRateSources.Blue,
        string timeZone = DefaultTimeZone)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return LedgerErrors.InvalidName;
        }

        if (!CurrencyCodes.IsValid(baseCurrency))
        {
            return LedgerErrors.InvalidCurrency;
        }

        var error = ValidateSettings(referenceRateSource, timeZone);
        if (error is not null)
        {
            return error;
        }

        var ledger = new Ledger(id, cleanName, baseCurrency, referenceRateSource, timeZone);
        ledger._members.Add(new LedgerMember(id, ownerUserId, LedgerRole.Owner));
        return ledger;
    }

    public Result ChangeSettings(string referenceRateSource, string timeZone)
    {
        var error = ValidateSettings(referenceRateSource, timeZone);
        if (error is not null)
        {
            return error;
        }

        ReferenceRateSource = referenceRateSource;
        TimeZone = timeZone;
        return Result.Success();
    }

    public Result AddMember(Guid userId, LedgerRole role)
    {
        if (role == LedgerRole.Owner)
        {
            return LedgerErrors.SingleOwner;
        }

        if (_members.Exists(member => member.UserId == userId))
        {
            return LedgerErrors.AlreadyMember;
        }

        _members.Add(new LedgerMember(Id, userId, role));
        return Result.Success();
    }

    /// <summary>
    /// La fecha de hoy en la zona horaria del libro. El servidor trabaja en UTC:
    /// a las 21 h del 31 de octubre en Argentina, en UTC ya es 1 de noviembre.
    /// </summary>
    public DateOnly TodayAt(DateTimeOffset now)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
    }

    public Period PeriodAt(DateTimeOffset now) => Period.FromDate(TodayAt(now));

    private static Error? ValidateSettings(string referenceRateSource, string timeZone)
    {
        if (!ExchangeRateSources.IsValid(referenceRateSource))
        {
            return LedgerErrors.InvalidRateSource;
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
        {
            return LedgerErrors.InvalidTimeZone;
        }

        return null;
    }
}
