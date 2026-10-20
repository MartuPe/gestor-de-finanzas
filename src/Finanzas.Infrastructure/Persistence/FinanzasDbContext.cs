using System.Linq.Expressions;
using Finanzas.Domain.Accounts;
using Finanzas.Domain.Categories;
using Finanzas.Domain.Common;
using Finanzas.Domain.Ledgers;
using Finanzas.Domain.Recurring;
using Finanzas.Domain.Statements;
using Finanzas.Domain.Tags;
using Finanzas.Domain.Transactions;
using Finanzas.Infrastructure.Identity;
using Finanzas.Infrastructure.Persistence.Conventions;
using Finanzas.Infrastructure.Persistence.Records;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Finanzas.Infrastructure.Persistence;

/// <summary>
/// La base de datos de la app. El esquema sigue docs/arquitectura/01-modelo-de-datos.md.
/// Incluye las tablas de ASP.NET Core Identity (usuarios y passkeys) y las claves de Data Protection.
/// </summary>
public sealed class FinanzasDbContext(DbContextOptions<FinanzasDbContext> options, ILedgerScope? ledgerScope = null)
    : IdentityUserContext<AppUser, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>Filtro global con nombre que acota cada consulta al libro del pedido (02-backend §5).</summary>
    public const string LedgerFilter = "Ledger";

    /// <summary>Entidades del dominio sin columnas de auditoría: las tablas de unión.</summary>
    private static readonly Type[] NotAudited = [typeof(LedgerMember), typeof(TransactionTag)];

    /// <summary>
    /// Las únicas relaciones propias que borran en cascada en la base: las cuotas y las etiquetas de un movimiento.
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

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>El libro del pedido. Lo lee el filtro global en cada consulta; null no filtra.</summary>
    internal Guid? CurrentLedgerId => ledgerScope?.LedgerId;

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
        base.OnModelCreating(modelBuilder);
        ConfigureIdentityTables(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanzasDbContext).Assembly);

        var model = modelBuilder.Model;
        var domainTypes = model.GetEntityTypes().Where(IsDomainEntity).ToList();
        AuditColumns.AddTo(domainTypes.Where(entityType => !NotAudited.Contains(entityType.ClrType)).ToList());
        AddLedgerFilters(domainTypes);

        // Solo las relaciones propias: las de Identity mantienen sus cascadas (borrar un usuario borra sus passkeys).
        foreach (var foreignKey in domainTypes.SelectMany(entityType => entityType.GetForeignKeys()))
        {
            var pair = (foreignKey.DeclaringEntityType.ClrType, foreignKey.PrincipalEntityType.ClrType);
            if (!foreignKey.IsOwnership && !Cascading.Contains(pair))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    /// <summary>
    /// Identity elige su esquema leyendo <see cref="IdentityOptions"/> del contenedor de la app, y solo el 3 tiene passkeys.
    /// Si el contexto se crea a mano (tests, herramientas) no hay contenedor: se usa uno mínimo con esas opciones,
    /// así el modelo es el mismo lo cree quien lo cree.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.Options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider is null)
        {
            optionsBuilder.UseApplicationServiceProvider(IdentityDefaults.Value);
        }
    }

    /// <summary>El esquema de Identity con la tabla de passkeys. Lo registra <c>AddInfrastructure</c>.</summary>
    internal static void UseIdentitySchema(IdentityOptions options) =>
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

    private static readonly Lazy<IServiceProvider> IdentityDefaults = new(() =>
        new ServiceCollection().Configure<IdentityOptions>(UseIdentitySchema).BuildServiceProvider());

    private static void ConfigureIdentityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(user =>
        {
            user.ToTable("Users");
            user.Property(appUser => appUser.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength);
        });
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityUserPasskey<Guid>>().ToTable("UserPasskeys");
        modelBuilder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");
    }

    /// <summary>
    /// Filtro <see cref="LedgerFilter"/> en las raíces con <c>LedgerId</c> y en <c>Ledgers</c>.
    /// Resúmenes y cuotas no lo necesitan: se leen siempre a través de una cuenta o un movimiento.
    /// </summary>
    private void AddLedgerFilters(IEnumerable<IMutableEntityType> entityTypes)
    {
        var currentLedger = Expression.Property(Expression.Constant(this), nameof(CurrentLedgerId));
        var noScope = Expression.Equal(currentLedger, Expression.Constant(null, typeof(Guid?)));

        foreach (var entityType in entityTypes.Where(entityType => !entityType.IsOwned()))
        {
            var keyName = entityType.ClrType == typeof(Ledger) ? nameof(Ledger.Id)
                : entityType.FindProperty("LedgerId") is not null ? "LedgerId"
                : null;
            if (keyName is null)
            {
                continue;
            }

            var entity = Expression.Parameter(entityType.ClrType, "e");
            var ledgerId = Expression.Convert(
                Expression.Call(typeof(EF), nameof(EF.Property), [typeof(Guid)], entity, Expression.Constant(keyName)),
                typeof(Guid?));
            var body = Expression.OrElse(noScope, Expression.Equal(ledgerId, currentLedger));
            entityType.SetQueryFilter(LedgerFilter, Expression.Lambda(body, entity));
        }
    }

    private static bool IsDomainEntity(IMutableEntityType entityType) =>
        entityType.ClrType.Namespace?.StartsWith("Finanzas.Domain", StringComparison.Ordinal) == true;

    private static void StoreAsSnakeCase<TEnum>(ModelConfigurationBuilder configurationBuilder)
        where TEnum : struct, Enum =>
        configurationBuilder.Properties<TEnum>()
            .HaveConversion<SnakeCaseEnumConverter<TEnum>>()
            .HaveMaxLength(20)
            .AreUnicode(false);
}
