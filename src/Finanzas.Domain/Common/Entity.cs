namespace Finanzas.Domain.Common;

/// <summary>Base de las entidades con identidad propia.</summary>
public abstract class Entity
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El Id no puede ser Guid.Empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Constructor para EF Core.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; private set; }
}
