using Finanzas.Application.Abstractions;
using Finanzas.Application.ExchangeRates;
using Finanzas.Infrastructure.ExchangeRates;
using Finanzas.Infrastructure.Persistence;
using Finanzas.Infrastructure.Persistence.Interceptors;
using Finanzas.Infrastructure.Persistence.Readers;
using Finanzas.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Finanzas.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Finanzas";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión '{ConnectionStringName}'. En desarrollo se configura con dotnet user-secrets (ver README).");

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AuditInterceptor>();
        services.AddDbContext<FinanzasDbContext>((provider, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IStatementRepository, StatementRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRecurringTemplateRepository, RecurringTemplateRepository>();

        services.AddScoped<IMonthReader, MonthReader>();
        services.AddScoped<IStatementReader, StatementReader>();
        services.AddScoped<ISpendingReader, SpendingReader>();

        services.AddScoped<IExchangeRateStore, ExchangeRateStore>();
        services.AddHttpClient<IExchangeRateSource, DolarApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["DolarApi:BaseUrl"] ?? "https://dolarapi.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
