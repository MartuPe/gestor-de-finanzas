namespace Finanzas.Infrastructure.Identity;

/// <summary>Claims propios que viajan en la cookie de sesión.</summary>
public static class AppClaims
{
    /// <summary>El libro del usuario: con esto la Api sabe sobre qué datos trabaja cada pedido.</summary>
    public const string LedgerId = "ledger_id";

    public const string DisplayName = "display_name";
}
