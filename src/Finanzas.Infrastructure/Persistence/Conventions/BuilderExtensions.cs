using Finanzas.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Conventions;

internal static class BuilderExtensions
{
    /// <summary>Código ISO 4217: <c>char(3)</c>.</summary>
    public static PropertyBuilder<string> IsCurrencyCode(this PropertyBuilder<string> property) =>
        property.HasMaxLength(3).IsFixedLength().IsUnicode(false);

    /// <summary>Color <c>#RRGGBB</c>: <c>char(7)</c>.</summary>
    public static PropertyBuilder<string?> IsHexColor(this PropertyBuilder<string?> property) =>
        property.HasMaxLength(7).IsFixedLength().IsUnicode(false);

    /// <summary>FK de una columna de moneda a la tabla <c>Currencies</c>.</summary>
    public static void HasCurrency<TEntity>(this EntityTypeBuilder<TEntity> builder, string currencyProperty)
        where TEntity : class =>
        builder.HasOne<CurrencyRecord>().WithMany().HasForeignKey(currencyProperty);
}
