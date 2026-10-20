using System.Text.Json;
using System.Text.Json.Serialization;
using Finanzas.Domain.Common;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Finanzas.Api.Http;

internal static class HttpSetup
{
    public static IServiceCollection AddAppHttp(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            // Los montos viajan como números JSON: "42000" entre comillas es un error, no un monto.
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;

            // Enums como en la base: "card_payment", "installment_credit".
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
            options.SerializerOptions.Converters.Add(new PeriodJsonConverter());
        });

        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            // Period se documenta como texto "yyyy-MM", que es como viaja. Va en línea en cada propiedad:
            // el transformador solo ve las propiedades, no el componente al que después apuntarían.
            options.CreateSchemaReferenceId = type =>
                IsPeriod(type.Type) ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(type);
            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (IsPeriod(context.JsonTypeInfo.Type))
                {
                    schema.Type = context.JsonTypeInfo.Type == typeof(Period)
                        ? JsonSchemaType.String
                        : JsonSchemaType.String | JsonSchemaType.Null;
                    schema.Pattern = @"^\d{4}-\d{2}$";
                    schema.Properties?.Clear();
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static bool IsPeriod(Type type) => (Nullable.GetUnderlyingType(type) ?? type) == typeof(Period);
}
