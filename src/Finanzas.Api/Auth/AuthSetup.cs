using System.Threading.RateLimiting;
using Finanzas.Application.Abstractions;
using Finanzas.Infrastructure;
using Finanzas.Infrastructure.Identity;
using Finanzas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Finanzas.Api.Auth;

internal static class AuthSetup
{
    /// <summary>Política de los endpoints de la app: sesión iniciada y con un libro.</summary>
    public const string LedgerPolicy = "Ledger";

    /// <summary>Límite de intentos en login, configuración inicial y passkeys (02-backend §5).</summary>
    public const string AuthRateLimit = "auth";

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUser>());
        services.AddScoped<ILedgerScope>(provider => provider.GetRequiredService<CurrentUser>());

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        // La PWA se sirve desde el mismo origen (02-backend §4.1): la sesión es una cookie que JavaScript no puede leer.
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "finanzas.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;

            // Es una API: sin sesión responde 401/403 en vez de redirigir a una página de login.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(LedgerPolicy, policy => policy.RequireAuthenticatedUser().RequireClaim(AppClaims.LedgerId));

        services.AddIdentityPersistence().AddSignInManager();

        var permitLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
