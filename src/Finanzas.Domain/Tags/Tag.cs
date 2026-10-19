using Finanzas.Domain.Common;

namespace Finanzas.Domain.Tags;

/// <summary>Con quién, para quién o para qué: "Lu", "Pinamar", "Gym". Es independiente de la categoría.</summary>
public sealed class Tag : Entity, IAggregateRoot
{
    public const int NameMaxLength = 40;

    private Tag()
    {
    }

    private Tag(Guid id, Guid ledgerId, string name, string? color)
        : base(id)
    {
        LedgerId = ledgerId;
        Name = name;
        Color = color;
    }

    public Guid LedgerId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Color { get; private set; }

    public static Result<Tag> Create(Guid id, Guid ledgerId, string name, string? color = null)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return TagErrors.InvalidName;
        }

        if (color is not null && !Text.IsHexColor(color))
        {
            return TagErrors.InvalidColor;
        }

        return new Tag(id, ledgerId, cleanName, color);
    }

    public Result Rename(string name)
    {
        var cleanName = Text.Clean(name);
        if (cleanName is null || cleanName.Length > NameMaxLength)
        {
            return TagErrors.InvalidName;
        }

        Name = cleanName;
        return Result.Success();
    }
}
