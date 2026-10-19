using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Recurring;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class RecurringTemplateConfiguration : IEntityTypeConfiguration<RecurringTemplate>
{
    public void Configure(EntityTypeBuilder<RecurringTemplate> builder)
    {
        builder.ToTable("RecurringTemplates", table =>
        {
            table.HasCheckConstraint("CK_RecurringTemplates_Kind", Sql.In<RecurringKind>(nameof(RecurringTemplate.Kind)));
            table.HasCheckConstraint(
                "CK_RecurringTemplates_Frequency", Sql.In<RecurringFrequency>(nameof(RecurringTemplate.Frequency)));
            table.HasCheckConstraint("CK_RecurringTemplates_Amount", "[EstimatedAmount] > 0");
            table.HasCheckConstraint(
                "CK_RecurringTemplates_Interval", $"[Interval] BETWEEN 1 AND {RecurringTemplate.MaxInterval}");
            table.HasCheckConstraint("CK_RecurringTemplates_Day", "[DayOfMonth] BETWEEN 1 AND 31");
            table.HasCheckConstraint("CK_RecurringTemplates_Dates", "[EndsOn] IS NULL OR [EndsOn] >= [StartsOn]");
            table.HasCheckConstraint(
                "CK_RecurringTemplates_Destination",
                Sql.Iff($"[Kind] = {Sql.Literal(RecurringKind.Transfer)}", "[ToAccountId] IS NOT NULL"));
        });

        builder.HasKey(template => template.Id);
        builder.Property(template => template.Id).ValueGeneratedNever();
        builder.HasOne<Ledger>().WithMany().HasForeignKey(template => template.LedgerId);
        builder.HasOne<Account>().WithMany().HasForeignKey(template => template.AccountId);
        builder.HasOne<Account>().WithMany().HasForeignKey(template => template.ToAccountId);
        builder.HasOne<Category>().WithMany().HasForeignKey(template => template.CategoryId);
        builder.Property(template => template.Description).HasMaxLength(RecurringTemplate.DescriptionMaxLength);
        builder.Property(template => template.Currency).IsCurrencyCode();
        builder.HasCurrency(nameof(RecurringTemplate.Currency));
        builder.Property(template => template.Interval).HasConversion<byte>();
        builder.Property(template => template.DayOfMonth).HasConversion<byte>();
    }
}
