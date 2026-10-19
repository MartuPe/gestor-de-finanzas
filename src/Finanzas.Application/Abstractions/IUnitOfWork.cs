using Finanzas.Domain.Common;

namespace Finanzas.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Guarda todo en una sola transacción. Los conflictos esperables (concurrencia, duplicados)
    /// vuelven como <see cref="Result"/>, no como excepciones.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
