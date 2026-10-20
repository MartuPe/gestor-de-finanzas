using Microsoft.AspNetCore.Identity;

namespace Finanzas.Infrastructure.Identity;

/// <summary>Usuario de ASP.NET Core Identity, guardado en la tabla <c>Users</c> (01-modelo §4.1).</summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 100;

    public string DisplayName { get; set; } = string.Empty;
}
