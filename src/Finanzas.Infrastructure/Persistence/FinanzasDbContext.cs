using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Tags;
using Finanzas.Domain.Transactions;
using Finanzas.Infrastructure.Persistence.Conventions;
using Finanzas.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence;

/// <summary>La base de datos de la app. El esquema sigue docs/arquitectura/01-modelo-de-datos.md.</summary>
public sealed class FinanzasDbContext(DbContextOptions<FinanzasDbContext> options) : DbContext(options)
{
    /// <summary>Tablas sin columnas de auditoría: las de unión y los datos de referencia.</summary>
    private static readonly Type[] NotAudited =
        [typeof(CurrencyRecord), typeof(ExchangeRateRecord), typeof(LedgerMember), typeof(TransactionTag)];

    /// <summary>
    /// Las únicas relaciones que borran en cascada en la base: las cuotas y las etiquetas de un movimiento.
    /// Las demás son Restrict, porque SQL Server no admite varios caminos de cascada hacia una misma tabla.
    /// </summary>
    private static readonly (Type Dependent, Type Principal)[] Cascading =
        [(typeof(Installment), typeof(Transaction)), (typeof(TransactionTag), typeof(Transaction))];

    public DbSet<Ledger> Ledgers => Set<Ledger>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<Statement> Statements => Set<Statement>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Installment> Installments => Set<Installment>();

    public DbSet<RecurringTemplate> RecurringTemplates => Set<RecurringTemplate>();

    public DbSet<CurrencyRecord> Currencies => Set<CurrencyRecord>();

    public DbSet<ExchangeRateRecord> ExchangeRates => Set<ExchangeRateRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<DateTime>().HaveColumnType("datetime2(3)");
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<UtcDateTimeOffsetConverter>()
            .HaveColumnType("datetime2(3)");
        configurationBuilder.Properties<Period>().HaveConversion<PeriodConverter>();

        StoreAsSnakeCase<AccountKind>(configurationBuilder);
        StoreAsSnakeCase<CreditLineType>(configurationBuilder);
        StoreAsSnakeCase<CategoryKind>(configurationBuilder);
        StoreAsSnakeCase<LedgerRole>(configurationBuilder);
        StoreAsSnakeCase<RecurringFrequency>(configurationBuilder);
        StoreAsSnakeCase<RecurringKind>(configurationBuilder);
        StoreAsSnakeCase<StatementStatus>(configurationBuilder);
        StoreAsSnakeCase<TransactionKind>(configurationBuilder);
        StoreAsSnakeCase<TransactionStatus>(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanzasDbContext).Assembly);

        var model = modelBuilder.Model;
        AuditColumns.AddTo(model.GetEntityTypes().Where(entityType => !NotAudited.Contains(entityType.ClrType)).ToList());

        foreach (var foreignKey in model.GetEntityTypes().SelectMany(entityType => entityType.GetForeignKeys()))
        {
            var pair = (foreignKey.DeclaringEntityType.ClrType, foreignKey.PrincipalEntityType.ClrType);
            if (!foreignKey.IsOwnership && !Cascading.Contains(pair))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    private static void StoreAsSnakeCase<TEnum>(ModelConfigurationBuilder configurationBuilder)
        where TEnum : struct, Enum =>
        configurationBuilder.Properties<TEnum>()
            .HaveConversion<SnakeCaseEnumConverter<TEnum>>()
            .HaveMaxLength(20)
            .AreUnicode(false);
}
