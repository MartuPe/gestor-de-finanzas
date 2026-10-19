using Finanzas.Infrastructure.Persistence;
using Finanzas.Infrastructure.Persistence.Interceptors;
using Finanzas.Infrastructure.Tests;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace Finanzas.Infrastructure.Tests;

/// <summary>
/// SQL Server real en Docker, con las migraciones aplicadas. Se levanta una sola vez para todos los tests;
/// cada test crea su propio libro, así no se pisan entre sí.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "FinanzasTests",
        }.ConnectionString;

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public FinanzasDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FinanzasDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new AuditInterceptor(TimeProvider.System))
            .Options);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
