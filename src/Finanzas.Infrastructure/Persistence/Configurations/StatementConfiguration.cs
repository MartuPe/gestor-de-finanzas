using Finanzas.Domain.Accounts;
using Finanzas.Domain.Statements;
using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finanzas.Infrastructure.Persistence.Configurations;

internal sealed class StatementConfiguration : IEntityTypeConfiguration<Statement>
{
    public void Configure(EntityTypeBuilder<Statement> builder)
    {
        builder.ToTable("Statements", table =>
        {
            table.HasCheckConstraint("CK_Statements_Status", Sql.In<StatementStatus>(nameof(Statement.Status)));
            table.HasCheckConstraint("CK_Statements_PeriodFirstDay", "DAY([Period]) = 1");
            table.HasCheckConstraint("CK_Statements_ClosingBeforeDue", "[ClosingDate] IS NULL OR [ClosingDate] < [DueDate]");
        });

        builder.HasKey(statement => statement.Id);
        builder.Property(statement => statement.Id).ValueGeneratedNever();

        // EF Core no admite FK hacia un tipo owned (CreditLines): apunta a Accounts, que tiene la misma clave.
        builder.HasOne<Account>().WithMany().HasForeignKey(statement => statement.CreditAccountId);
        builder.Property(statement => statement.Note).HasMaxLength(Statement.NoteMaxLength);

        builder.HasIndex(statement => new { statement.CreditAccountId, statement.Period }).IsUnique();
    }
}
