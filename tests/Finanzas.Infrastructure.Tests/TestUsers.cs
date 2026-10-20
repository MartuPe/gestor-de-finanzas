using Finanzas.Infrastructure.Identity;

namespace Finanzas.Infrastructure.Tests;

/// <summary>Usuarios mínimos para la FK de <c>LedgerMembers.UserId</c>. Sin contraseña: no inician sesión.</summary>
internal static class TestUsers
{
    public static AppUser New(Guid id)
    {
        var email = $"{id:N}@test.local";
        return new AppUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = "Martu",
            SecurityStamp = Guid.NewGuid().ToString(),
        };
    }

    public static async Task<Guid> CreateAsync(SqlServerFixture fixture, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await using var db = fixture.CreateContext();
        db.Users.Add(New(id));
        await db.SaveChangesAsync(cancellationToken);
        return id;
    }
}
