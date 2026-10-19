namespace Finanzas.Application.Abstractions;

/// <summary>Quién hace el pedido y sobre qué libro. Lo implementa la Api a partir de la sesión.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    Guid LedgerId { get; }
}
