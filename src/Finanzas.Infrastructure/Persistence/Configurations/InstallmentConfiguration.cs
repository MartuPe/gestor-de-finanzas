using Finanzas.Domain.Statements;
using Finanzas.Domain.Transactions;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("Installments", table =>
        {
            table.HasCheckConstraint("CK_Installments_Number", "[Number] >= 1 AND [Number] <= [Total]");
            table.HasCheckConstraint("CK_Installments_AmountNotZero", "[Amount] <> 0");
        });

        builder.HasKey(installment => installment.Id);
        builder.Property(installment => installment.Id).ValueGeneratedNever();
        builder.HasOne<Statement>().WithMany().HasForeignKey(installment => installment.StatementId);
        builder.Property(installment => installment.Number).HasConversion<short>();
        builder.Property(installment => installment.Total).HasConversion<short>();
        builder.Property(installment => installment.Currency).IsCurrencyCode();
        builder.HasCurrency(nameof(Installment.Currency));

        builder.HasIndex(installment => new { installment.TransactionId, installment.Number }).IsUnique();
    }
}
