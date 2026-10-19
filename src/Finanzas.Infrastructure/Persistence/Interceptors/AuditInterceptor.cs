using Finanzas.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Finanzas.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Completa <c>CreatedAt</c> y <c>UpdatedAt</c>, y convierte los borrados en borrado lógico (<c>DeletedAt</c>).
/// Las tablas sin auditoría (unión, monedas, cotizaciones) se borran de verdad, salvo que dependan
/// de algo que se está borrando lógicamente: así un "deshacer" recupera también sus etiquetas.
/// </summary>
internal sealed class AuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var entries = context.ChangeTracker.Entries().ToList();
        var softDeleted = entries
            .Where(entry => entry.State == EntityState.Deleted && IsAudited(entry))
            .Select(entry => (entry.Metadata, Key: SingleKey(entry)))
            .ToHashSet();

        foreach (var entry in entries)
        {
            if (!IsAudited(entry))
            {
                if (entry.State == EntityState.Deleted && DependsOnAny(entry, softDeleted))
                {
                    entry.State = EntityState.Unchanged;
                }

                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(AuditColumns.CreatedAt).CurrentValue = now;
                    break;

                case EntityState.Modified:
                    entry.Property(AuditColumns.UpdatedAt).CurrentValue = now;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(AuditColumns.DeletedAt).CurrentValue = now;
                    entry.Property(AuditColumns.UpdatedAt).CurrentValue = now;
                    break;
            }
        }
    }

    private static bool IsAudited(EntityEntry entry) => entry.Metadata.FindProperty(AuditColumns.CreatedAt) is not null;

    private static object? SingleKey(EntityEntry entry) =>
        entry.Metadata.FindPrimaryKey() is { Properties: [var key] } ? entry.Property(key.Name).CurrentValue : null;

    private static bool DependsOnAny(EntityEntry entry, HashSet<(IEntityType Metadata, object? Key)> softDeleted) =>
        entry.Metadata.GetForeignKeys().Any(foreignKey =>
            foreignKey.Properties is [var property]
            && softDeleted.Contains((foreignKey.PrincipalEntityType, entry.Property(property.Name).CurrentValue)));
}
