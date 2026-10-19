namespace Finanzas.Application.Common;

/// <summary>
/// Resultado de un alta idempotente: <see cref="Created"/> es false cuando el mismo pedido ya se había
/// guardado (el celular reintentó) y se devuelve lo existente. La Api responde 201 o 200 según el caso.
/// </summary>
public sealed record Registration<T>(T Value, bool Created);
