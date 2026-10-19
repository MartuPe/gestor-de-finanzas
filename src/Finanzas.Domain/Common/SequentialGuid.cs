using System.Buffers.Binary;

namespace Finanzas.Domain.Common;

/// <summary>
/// GUID que SQL Server ordena en forma creciente, para no fragmentar los índices.
/// SQL Server compara primero los últimos 6 bytes y después los bytes 8 y 9:
/// ahí va un contador que arranca en los ticks del reloj. Es el mismo esquema que usa EF Core.
/// </summary>
public static class SequentialGuid
{
    private static long _counter = DateTime.UtcNow.Ticks;

    public static Guid New()
    {
        Span<byte> guid = stackalloc byte[16];
        Guid.NewGuid().TryWriteBytes(guid);

        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(counter, Interlocked.Increment(ref _counter));

        guid[8] = counter[1];
        guid[9] = counter[0];
        guid[10] = counter[7];
        guid[11] = counter[6];
        guid[12] = counter[5];
        guid[13] = counter[4];
        guid[14] = counter[3];
        guid[15] = counter[2];

        return new Guid(guid);
    }
}
