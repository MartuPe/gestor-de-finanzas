using Finanzas.Application.Abstractions;
using Finanzas.Application.ExchangeRates;
using Finanzas.Infrastructure.ExchangeRates;
using Finanzas.Infrastructure.Identity;
using Finanzas.Infrastructure.Persistence;
using Finanzas.Infrastructure.Persistence.Interceptors;
using Finanzas.Infrastructure.Persistence.Readers;
using Finanzas.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
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
        services.Configure<IdentityOptions>(FinanzasDbContext.UseIdentitySchema);
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
        services.AddScoped<IAccountReader, AccountReader>();
        services.AddScoped<ITransactionReader, TransactionReader>();

        services.AddScoped<IExchangeRateStore, ExchangeRateStore>();
        services.AddHttpClient<IExchangeRateSource, DolarApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["DolarApi:BaseUrl"] ?? "https://dolarapi.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Claves que firman la cookie de sesión: en la base, para que sobrevivan a que el contenedor se reinicie.
        services.AddDataProtection()
            .SetApplicationName("Finanzas")
            .PersistKeysToDbContext<FinanzasDbContext>();

        return services;
    }

    /// <summary>
    /// Usuarios de ASP.NET Core Identity guardados en <see cref="FinanzasDbContext"/>, con passkeys
    /// (el esquema 3 ya lo fija <see cref="AddInfrastructure"/>, porque define el modelo de la base).
    /// La Api completa la configuración con <c>AddSignInManager()</c>.
    /// </summary>
    public static IdentityBuilder AddIdentityPersistence(this IServiceCollection services)
    {
        services.AddScoped<OwnerSetup>();
        return services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // La contraseña es el respaldo de las passkeys: larga antes que rebuscada (NIST SP 800-63B).
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<FinanzasDbContext>()
            .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();
    }
}
