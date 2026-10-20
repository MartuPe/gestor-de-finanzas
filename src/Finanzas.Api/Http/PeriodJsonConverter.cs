using System.Text.Json;
using System.Text.Json.Serialization;
using Finanzas.Domain.Common;

namespace Finanzas.Api.Http;

/// <summary>Un <see cref="Period"/> viaja como texto <c>"2026-10"</c>, igual que en las rutas.</summary>
internal sealed class PeriodJsonConverter : JsonConverter<Period>
{
    public override Period Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Period.TryParse(reader.GetString(), out var period)
            ? period
            : throw new JsonException("El período tiene que tener el formato yyyy-MM.");

    public override void Write(Utf8JsonWriter writer, Period value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
