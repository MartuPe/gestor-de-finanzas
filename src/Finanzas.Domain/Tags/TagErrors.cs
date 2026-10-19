using Finanzas.Domain.Common;

namespace Finanzas.Domain.Tags;

public static class TagErrors
{
    public static readonly Error InvalidName =
        Error.Validation("tag.invalid_name", $"La etiqueta necesita un nombre de hasta {Tag.NameMaxLength} caracteres.");

    public static readonly Error InvalidColor =
        Error.Validation("tag.invalid_color", "El color tiene que tener el formato #RRGGBB.");
}
