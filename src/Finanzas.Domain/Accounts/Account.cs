using Finanzas.Domain.Common;

namespace Finanzas.Domain.Accounts;

/// <summary>
/// De dónde sale o adónde entra la plata: banco, efectivo, billetera, ahorro o una línea de crédito.
/// El medio de pago de un movimiento es su cuenta.
/// </summary>
public sealed class Account : Entity, IAggregateRoot
{
    public const int NameMaxLength = 60;

    private Account()
    {
    }

    private Account(
        Guid id,
        Guid ledgerId,
        string name,
        AccountKind kind,
        string defaultCurrency,
        bool isLiquid,
        bool earnsYield)
        : base(id)
    {
        LedgerId = ledgerId;
        Name = name;
        Kind = kind;
        DefaultCurrency = defaultCurrency;
        IsLiquid = isLiquid;
        EarnsYield = earnsYield;
    }

    public Guid LedgerId { get; private set; }

    public string Name { get; private set; } = null!;

    public AccountKind Kind { get; private set; }

    public string DefaultCurrency { get; private set; } = null!;

    /// <summary>Si entra en el saldo total y en el disponible. El ahorro y el crédito no.</summary>
    public bool IsLiquid { get; private set; }

    public bool EarnsYield { get; private set; }

    public CreditLine? CreditLine { get; private set; }

    public string? Color { get; private set; }

    public string? Icon { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsCredit => Kind == AccountKind.Credit;

    public bool IsArchived => ArchivedAt is not null;

    /// <summary>Cuentas de dinero. Las de crédito se crean con <see cref="CreateCredit"/>.</summary>
    public static Result<Account> Create(
        Guid id,
        Guid ledgerId,
        string name,
        AccountKind kind,
        string defaultCurrency,
        bool? isLiquid = null,
        bool earnsYield = false)
    {
        if (kind == AccountKind.Credit)
        {
            return AccountErrors.UseCreateCredit;
        }

        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return AccountErrors.InvalidName;
        }

        if (!CurrencyCodes.IsValid(defaultCurrency))
        {
            return AccountErrors.InvalidCurrency;
        }

        var liquid = isLiquid ?? kind != AccountKind.Savings;
        return new Account(id, ledgerId, cleanName, kind, defaultCurrency, liquid, earnsYield);
    }

    public static Result<Account> CreateCredit(
        Guid id,
        Guid ledgerId,
        string name,
        string defaultCurrency,
        CreditLineType lineType,
        string issuer,
        string? network,
        int? usualClosingDay,
        int usualDueDay)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return AccountErrors.InvalidName;
        }

        if (!CurrencyCodes.IsValid(defaultCurrency))
        {
            return AccountErrors.InvalidCurrency;
        }

        var line = CreditLine.Create(id, lineType, issuer, network, usualClosingDay, usualDueDay);
        if (line.IsFailure)
        {
            return line.Error;
        }

        return new Account(id, ledgerId, cleanName, AccountKind.Credit, defaultCurrency, isLiquid: false, earnsYield: false)
        {
            CreditLine = line.Value,
        };
    }

    public Result Rename(string name)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return AccountErrors.InvalidName;
        }

        Name = cleanName;
        return Result.Success();
    }

    public Result ChangeAppearance(string? color, string? icon)
    {
        if (color is not null && !Text.IsHexColor(color))
        {
            return AccountErrors.InvalidColor;
        }

        Color = color;
        Icon = Text.Clean(icon);
        return Result.Success();
    }

    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;

    /// <summary>La cuenta se cerró: deja de mostrarse, pero conserva su historia.</summary>
    public void Archive(DateTimeOffset at) => ArchivedAt ??= at;

    public void Unarchive() => ArchivedAt = null;
}
