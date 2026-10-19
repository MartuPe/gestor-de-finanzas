using Finanzas.Application.Abstractions;
using Finanzas.Application.Common;
using Finanzas.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Finanzas.Infrastructure.Persistence.Repositories;

internal sealed class UnitOfWork(FinanzasDbContext db) : IUnitOfWork
{
    /// <summary>Números de error de SQL Server para claves duplicadas (índice único y PK/UNIQUE).</summary>
    private static readonly int[] DuplicateKeyErrors = [2601, 2627];

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ApplicationErrors.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql && DuplicateKeyErrors.Contains(sql.Number))
        {
            return ApplicationErrors.Duplicate;
        }
    }
}
