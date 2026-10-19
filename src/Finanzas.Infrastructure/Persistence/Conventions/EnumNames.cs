using System.Text;

namespace Finanzas.Infrastructure.Persistence.Conventions;

/// <summary>
/// Nombre en la base de cada valor de un enum: snake_case en minúsculas
/// (<c>TransactionKind.CardPayment</c> se guarda como <c>card_payment</c>), como en el modelo de datos.
/// </summary>
internal static class EnumNames<TEnum>
    where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> ToDb =
        Enum.GetValues<TEnum>().ToDictionary(value => value, value => SnakeCase(value.ToString()));

    private static readonly Dictionary<string, TEnum> FromDb =
        ToDb.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public static IReadOnlyCollection<string> Values => ToDb.Values;

    public static string ToDatabase(TEnum value) => ToDb[value];

    public static TEnum FromDatabase(string value) => FromDb[value];

    private static string SnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }
}
