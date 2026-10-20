using Finanzas.Application.Common;
using Finanzas.Application.Ledgers.CreateLedger;
using Finanzas.Domain.Common;
using Finanzas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Identity;

/// <summary>
/// Configuración inicial (02-backend §5, registro cerrado): crea el único usuario y su libro, en una sola
/// transacción. Una vez que existe un usuario, no se puede volver a usar.
/// </summary>
public sealed class OwnerSetup(FinanzasDbContext db, UserManager<AppUser> users, CreateLedgerHandler createLedger)
{
    public async Task<Result<AppUser>> CreateAsync(
        string email,
        string password,
        string displayName,
        string ledgerName,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return AuthErrors.AlreadySetUp;
        }

        // Con reintentos de conexión activados, una transacción propia tiene que correr dentro de la estrategia.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var user = new AppUser { Id = SequentialGuid.New(), UserName = email, Email = email, DisplayName = displayName.Trim() };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                return Result<AppUser>.Failure(ToValidationError(created));
            }

            var ledger = await createLedger.HandleAsync(new CreateLedgerCommand(SequentialGuid.New(), user.Id, ledgerName), cancellationToken);
            if (ledger.IsFailure)
            {
                return Result<AppUser>.Failure(ledger.Error);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<AppUser>.Success(user);
        });
    }

    /// <summary>Los errores de Identity (contraseña corta, mail repetido…) con el campo al que corresponden.</summary>
    private static ValidationError ToValidationError(IdentityResult result) =>
        new(result.Errors
            .GroupBy(error => error.Code.StartsWith("Password", StringComparison.Ordinal) ? "Password" : "Email")
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray())
            .AsReadOnly());
}
