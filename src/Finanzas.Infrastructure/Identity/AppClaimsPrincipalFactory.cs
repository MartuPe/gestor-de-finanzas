using System.Security.Claims;
using Finanzas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Finanzas.Infrastructure.Identity;

/// <summary>Agrega a la sesión el libro del usuario y su nombre, para no buscarlos en cada pedido.</summary>
public sealed class AppClaimsPrincipalFactory(
    UserManager<AppUser> userManager,
    IOptions<IdentityOptions> options,
    FinanzasDbContext db)
    : UserClaimsPrincipalFactory<AppUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(AppClaims.DisplayName, user.DisplayName));

        // Al iniciar sesión todavía no hay libro elegido: se busca sin el filtro por libro.
        var ledgerId = await db.Ledgers
            .IgnoreQueryFilters()
            .Where(ledger => ledger.Members.Any(member => member.UserId == user.Id))
            .Select(ledger => (Guid?)ledger.Id)
            .FirstOrDefaultAsync();
        if (ledgerId is Guid id)
        {
            identity.AddClaim(new Claim(AppClaims.LedgerId, id.ToString()));
        }

        return identity;
    }
}
