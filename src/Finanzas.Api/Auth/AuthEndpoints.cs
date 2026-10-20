using System.Buffers.Text;
using System.Security.Claims;
using Finanzas.Api.Http;
using Finanzas.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Api.Auth;

/// <summary>
/// Sesión y passkeys (02-backend §5). El flujo de una passkey en la PWA es el de la plantilla oficial de .NET 10:
/// <list type="number">
/// <item>pedir las opciones (<c>…/creation-options</c> o <c>…/request-options</c>);</item>
/// <item>pasarlas por <c>PublicKeyCredential.parseCreationOptionsFromJSON</c> (o <c>parseRequestOptionsFromJSON</c>) y llamar a <c>navigator.credentials.create</c> (o <c>get</c>);</item>
/// <item>mandar el resultado con <c>JSON.stringify</c> a <c>/api/auth/passkeys</c> o a <c>/api/auth/passkeys/login</c>.</item>
/// </list>
/// </summary>
internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Sesión");

        auth.MapGet("/setup", async (UserManager<AppUser> users, CancellationToken cancellationToken) =>
            TypedResults.Ok(new SetupStatusResponse(Needed: !await users.Users.AnyAsync(cancellationToken))));

        auth.MapPost("/setup", async (
                SetupRequest request,
                OwnerSetup setup,
                SignInManager<AppUser> signIn,
                CancellationToken cancellationToken) =>
            {
                var created = await setup.CreateAsync(
                    request.Email, request.Password, request.DisplayName, request.LedgerName ?? "Mis finanzas", cancellationToken);
                if (created.IsFailure)
                {
                    return created.Error.ToProblem();
                }

                await signIn.SignInAsync(created.Value, isPersistent: true);
                return Results.NoContent();
            })
            .RequireRateLimiting(AuthSetup.AuthRateLimit);

        auth.MapPost("/login", async (LoginRequest request, SignInManager<AppUser> signIn) =>
            {
                var result = await signIn.PasswordSignInAsync(request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);
                return result switch
                {
                    { Succeeded: true } => Results.NoContent(),
                    { IsLockedOut: true } => AuthErrors.LockedOut.ToProblem(),
                    _ => Unauthorized(AuthErrors.InvalidCredentials.Code, AuthErrors.InvalidCredentials.Message),
                };
            })
            .RequireRateLimiting(AuthSetup.AuthRateLimit);

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
            {
                await signIn.SignOutAsync();
                return Results.NoContent();
            })
            .RequireAuthorization();

        auth.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
            {
                var user = await users.GetUserAsync(principal);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var passkeys = await users.GetPasskeysAsync(user);
                var ledgerId = Guid.TryParse(principal.FindFirstValue(AppClaims.LedgerId), out var id) ? id : (Guid?)null;
                return Results.Ok(new MeResponse(user.Id, user.Email ?? string.Empty, user.DisplayName, ledgerId, passkeys.Count));
            })
            .RequireAuthorization()
            .Returns<MeResponse>();

        MapPasskeys(auth);
    }

    private static void MapPasskeys(RouteGroupBuilder auth)
    {
        var passkeys = auth.MapGroup("/passkeys");

        // Alta: requiere sesión (se registra después de entrar con la contraseña, o desde otro dispositivo ya registrado).
        passkeys.MapPost("/creation-options", async (ClaimsPrincipal principal, UserManager<AppUser> users, SignInManager<AppUser> signIn) =>
            {
                var user = await users.GetUserAsync(principal);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var options = await signIn.MakePasskeyCreationOptionsAsync(new()
                {
                    Id = user.Id.ToString(),
                    Name = user.Email ?? user.Id.ToString(),
                    DisplayName = user.DisplayName,
                });
                return TypedResults.Content(options, contentType: "application/json");
            })
            .RequireAuthorization();

        passkeys.MapPost("/", async (
                PasskeyCredentialRequest request,
                ClaimsPrincipal principal,
                UserManager<AppUser> users,
                SignInManager<AppUser> signIn) =>
            {
                var user = await users.GetUserAsync(principal);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                var attestation = await signIn.PerformPasskeyAttestationAsync(request.CredentialJson);
                if (!attestation.Succeeded)
                {
                    return AuthErrors.PasskeyFailed.ToProblem();
                }

                attestation.Passkey.Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
                var added = await users.AddOrUpdatePasskeyAsync(user, attestation.Passkey);
                return added.Succeeded
                    ? TypedResults.Created((string?)null, ToResponse(attestation.Passkey))
                    : AuthErrors.PasskeyFailed.ToProblem();
            })
            .RequireAuthorization()
            .Produces<PasskeyResponse>(StatusCodes.Status201Created);

        passkeys.MapGet("/", async (ClaimsPrincipal principal, UserManager<AppUser> users) =>
            {
                var user = await users.GetUserAsync(principal);
                return user is null
                    ? Results.Unauthorized()
                    : Results.Ok((await users.GetPasskeysAsync(user)).Select(ToResponse).ToList());
            })
            .RequireAuthorization()
            .Returns<IReadOnlyList<PasskeyResponse>>();

        passkeys.MapDelete("/{credentialId}", async (
                string credentialId,
                ClaimsPrincipal principal,
                UserManager<AppUser> users) =>
            {
                var user = await users.GetUserAsync(principal);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                byte[] id;
                try
                {
                    id = Base64Url.DecodeFromChars(credentialId);
                }
                catch (FormatException)
                {
                    return Results.NotFound();
                }

                var removed = await users.RemovePasskeyAsync(user, id);
                return removed.Succeeded ? Results.NoContent() : Results.NotFound();
            })
            .RequireAuthorization();

        // Ingreso con passkey: sin sesión, con límite de intentos.
        passkeys.MapPost("/request-options", async (
                PasskeyRequestOptionsRequest request,
                UserManager<AppUser> users,
                SignInManager<AppUser> signIn) =>
            {
                var user = string.IsNullOrWhiteSpace(request.Email) ? null : await users.FindByEmailAsync(request.Email);
                var options = await signIn.MakePasskeyRequestOptionsAsync(user);
                return TypedResults.Content(options, contentType: "application/json");
            })
            .RequireRateLimiting(AuthSetup.AuthRateLimit);

        passkeys.MapPost("/login", async (PasskeyCredentialRequest request, SignInManager<AppUser> signIn) =>
            {
                var result = await signIn.PasskeySignInAsync(request.CredentialJson);
                return result.Succeeded
                    ? Results.NoContent()
                    : Unauthorized(AuthErrors.PasskeyFailed.Code, AuthErrors.PasskeyFailed.Message);
            })
            .RequireRateLimiting(AuthSetup.AuthRateLimit);
    }

    private static PasskeyResponse ToResponse(UserPasskeyInfo passkey) =>
        new(Base64Url.EncodeToString(passkey.CredentialId), passkey.Name);

    private static IResult Unauthorized(string code, string message) =>
        TypedResults.Problem(
            title: message,
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
