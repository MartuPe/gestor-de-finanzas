using Finanzas.Api.IntegrationTests;
using Finanzas.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]

namespace Finanzas.Api.IntegrationTests;

/// <summary>
/// Un SQL Server en Docker para todos los tests. Como el registro está cerrado (un solo usuario por base),
/// cada test usa su propia base de datos.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    /// <summary>
    /// Una base nueva con las migraciones aplicadas. Tiene que existir antes de levantar la Api: al arrancar,
    /// Data Protection lee sus claves y, si la base no existe, EF lo reintenta como un error transitorio por casi dos minutos.
    /// </summary>
    public async Task<string> NewDatabaseAsync(CancellationToken cancellationToken)
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"Api_{Guid.NewGuid():N}",
        }.ConnectionString;

        await using var db = new FinanzasDbContext(
            new DbContextOptionsBuilder<FinanzasDbContext>().UseSqlServer(connectionString).Options);
        await db.Database.MigrateAsync(cancellationToken);
        return connectionString;
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
