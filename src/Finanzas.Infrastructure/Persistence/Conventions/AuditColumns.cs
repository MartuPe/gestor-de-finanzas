using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Finanzas.Infrastructure.Persistence.Conventions;

/// <summary>
/// Columnas de auditoría (01-modelo §4.0). Son shadow properties: el dominio no las conoce;
/// las completa <see cref="Interceptors.AuditInterceptor"/>.
/// </summary>
internal static class AuditColumns
{
    public const string CreatedAt = nameof(CreatedAt);
    public const string UpdatedAt = nameof(UpdatedAt);
    public const string DeletedAt = nameof(DeletedAt);
    public const string RowVersion = nameof(RowVersion);

    /// <summary>Filtro global con nombre: se puede apagar solo este con <c>IgnoreQueryFilters([SoftDeleteFilter])</c>.</summary>
    public const string SoftDeleteFilter = "SoftDelete";

    /// <summary>
    /// Agrega las columnas a cada tipo, y el filtro de borrado lógico a los que no son owned
    /// (EF Core no admite filtros en tipos owned: se filtran junto con su dueño).
    /// </summary>
    public static void AddTo(IEnumerable<IMutableEntityType> entityTypes)
    {
        foreach (var entityType in entityTypes)
        {
            var createdAt = entityType.AddProperty(CreatedAt, typeof(DateTime));
            createdAt.SetColumnType("datetime2(3)");
            createdAt.SetDefaultValueSql("SYSUTCDATETIME()");

            entityType.AddProperty(UpdatedAt, typeof(DateTime?)).SetColumnType("datetime2(3)");
            entityType.AddProperty(DeletedAt, typeof(DateTime?)).SetColumnType("datetime2(3)");

            var rowVersion = entityType.AddProperty(RowVersion, typeof(byte[]));
            rowVersion.IsNullable = false;
            rowVersion.IsConcurrencyToken = true;
            rowVersion.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            rowVersion.SetColumnType("rowversion");

            if (!entityType.IsOwned())
            {
                entityType.SetQueryFilter(SoftDeleteFilter, NotDeleted(entityType.ClrType));
            }
        }
    }

    /// <summary><c>e =&gt; EF.Property&lt;DateTime?&gt;(e, "DeletedAt") == null</c> para el tipo dado.</summary>
    private static LambdaExpression NotDeleted(Type clrType)
    {
        var entity = Expression.Parameter(clrType, "e");
        var deletedAt = Expression.Call(
            typeof(EF), nameof(EF.Property), [typeof(DateTime?)], entity, Expression.Constant(DeletedAt));
        return Expression.Lambda(Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTime?))), entity);
    }
}
