using Finanzas.Domain.Common;

namespace Finanzas.Infrastructure.Identity;

public static class AuthErrors
{
    public static readonly Error AlreadySetUp =
        Error.Conflict("auth.already_set_up", "La app ya tiene su usuario: el registro está cerrado.");

    public static readonly Error InvalidCredentials =
        Error.Validation("auth.invalid_credentials", "El mail o la contraseña no son correctos.");

    public static readonly Error LockedOut =
        Error.Conflict("auth.locked_out", "Demasiados intentos fallidos. Probá de nuevo en unos minutos.");

    public static readonly Error PasskeyFailed =
        Error.Validation("auth.passkey_failed", "No se pudo verificar la passkey.");
}
