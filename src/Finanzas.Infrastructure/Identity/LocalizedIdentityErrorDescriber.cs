using Microsoft.AspNetCore.Identity;

namespace Finanzas.Infrastructure.Identity;

/// <summary>
/// Los mensajes de Identity en castellano. Solo los que puede ver alguien con las reglas de esta app
/// (contraseña larga, sin reglas de composición; el usuario es el mail).
/// </summary>
internal sealed class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"La contraseña tiene que tener al menos {length} caracteres." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "El mail no es válido." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "Ese mail ya está registrado." };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = "El mail no es válido." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "Ese mail ya está registrado." };
}
