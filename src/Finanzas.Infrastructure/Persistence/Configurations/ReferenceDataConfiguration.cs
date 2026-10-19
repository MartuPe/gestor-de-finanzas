using Finanzas.Domain.Common;
using Finanzas.Infrastructure.Persistence.Conventions;
using Finanzas.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<CurrencyRecord>
{
    public void Configure(EntityTypeBuilder<CurrencyRecord> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(currency => currency.Code);
        builder.Property(currency => currency.Code).IsCurrencyCode();
        builder.Property(currency => currency.Name).HasMaxLength(50);
        builder.Property(currency => currency.Symbol).HasMaxLength(5);

        builder.HasData(
            new CurrencyRecord { Code = CurrencyCodes.Ars, Name = "Peso argentino", Symbol = "$" },
            new CurrencyRecord { Code = CurrencyCodes.Usd, Name = "Dólar estadounidense", Symbol = "US$" });
    }
}

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRateRecord>
{
    public void Configure(EntityTypeBuilder<ExchangeRateRecord> builder)
    {
        builder.ToTable("ExchangeRates", table =>
        {
            table.HasCheckConstraint(
                "CK_ExchangeRates_Source", Sql.In(nameof(ExchangeRateRecord.Source), ExchangeRateSources.All));
            table.HasCheckConstraint("CK_ExchangeRates_Positive", "[Buy] > 0 AND [Sell] > 0");
        });

        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.Currency).IsCurrencyCode();
        builder.HasCurrency(nameof(ExchangeRateRecord.Currency));
        builder.Property(rate => rate.Source).HasMaxLength(20).IsUnicode(false);
        builder.Property(rate => rate.Buy).HasPrecision(18, 6);
        builder.Property(rate => rate.Sell).HasPrecision(18, 6);

        // R9 busca la última cotización con fecha <= D para una casa: este índice la cubre.
        builder.HasIndex(rate => new { rate.Currency, rate.Source, rate.RateDate }).IsUnique();
    }
}
