namespace Finanzas.Api.Auth;

/// <summary>Configuración inicial: el único usuario y su libro.</summary>
public sealed record SetupRequest(string Email, string Password, string DisplayName, string? LedgerName = null);

public sealed record SetupStatusResponse(bool Needed);

public sealed record LoginRequest(string Email, string Password);

/// <param name="CredentialJson">Lo que devuelve <c>navigator.credentials.create()</c> o <c>get()</c>, serializado con <c>JSON.stringify</c>.</param>
/// <param name="Name">Nombre para reconocer la passkey ("iPhone", "Notebook").</param>
public sealed record PasskeyCredentialRequest(string CredentialJson, string? Name = null);

/// <param name="Email">Opcional: sin mail, el navegador ofrece las passkeys guardadas para este sitio.</param>
public sealed record PasskeyRequestOptionsRequest(string? Email = null);

/// <param name="CredentialId">En Base64Url.</param>
public sealed record PasskeyResponse(string CredentialId, string? Name);

public sealed record MeResponse(Guid UserId, string Email, string DisplayName, Guid? LedgerId, int Passkeys);
