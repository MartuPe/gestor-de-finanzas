namespace Finanzas.Infrastructure.Persistence.Conventions;

/// <summary>Fragmentos de SQL para los CHECK del modelo de datos.</summary>
internal static class Sql
{
    /// <summary><c>[Columna] IN ('valor_1', 'valor_2', …)</c> con los valores del enum.</summary>
    public static string In<TEnum>(string column)
        where TEnum : struct, Enum =>
        In(column, EnumNames<TEnum>.Values);

    public static string In(string column, IEnumerable<string> values) =>
        $"[{column}] IN ({string.Join(", ", values.Select(value => $"'{value}'"))})";

    /// <summary>El valor del enum como literal SQL: <c>'card_payment'</c>.</summary>
    public static string Literal<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        $"'{EnumNames<TEnum>.ToDatabase(value)}'";

    /// <summary>T-SQL no compara booleanos: <c>(A) ⇔ (B)</c> se escribe comparando dos CASE.</summary>
    public static string Iff(string left, string right) =>
        $"CASE WHEN {left} THEN 1 ELSE 0 END = CASE WHEN {right} THEN 1 ELSE 0 END";
}
