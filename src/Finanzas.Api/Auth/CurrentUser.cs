using System.Security.Claims;
using Finanzas.Application.Abstractions;
using Finanzas.Infrastructure.Identity;
using Finanzas.Infrastructure.Persistence;

namespace Finanzas.Api.Auth;

/// <summary>
/// El usuario y el libro del pedido, leídos de la cookie de sesión. Para los casos de uso
/// (<see cref="ICurrentUser"/>) y para el filtro global por libro de EF Core (<see cref="ILedgerScope"/>).
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser, ILedgerScope
{
    public Guid UserId => Read(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No hay una sesión iniciada.");

    public Guid LedgerId => Read(AppClaims.LedgerId)
        ?? throw new InvalidOperationException("La sesión no tiene un libro asociado.");

    /// <summary>Sin sesión (por ejemplo, durante el login) no hay libro y el filtro no restringe.</summary>
    Guid? ILedgerScope.LedgerId => Read(AppClaims.LedgerId);

    private Guid? Read(string claimType) =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(claimType), out var value) ? value : null;
}
