using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Cadroue.Application;

public static class LGate
{
    private static ReadOnlySpan<byte> LGateMarker => "CADG"u8;

    public static Guid LGateBatchCreate()
    {
        Span<byte> lGateBytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt64BigEndian(lGateBytes, DateTimeOffset.UtcNow.UtcTicks);
        LGateMarker.CopyTo(lGateBytes[8..12]);
        RandomNumberGenerator.Fill(lGateBytes[12..]);
        return new Guid(lGateBytes);
    }

    public static DateTimeOffset LGateTimeRead(Guid lGateBatchId)
    {
        byte[] lGateBytes = lGateBatchId.ToByteArray();
        if (!lGateBytes.AsSpan(8, 4).SequenceEqual(LGateMarker))
        {
            return DateTimeOffset.MinValue;
        }

        long lGateTicks = BinaryPrimitives.ReadInt64BigEndian(lGateBytes);
        if (lGateTicks < 0 || lGateTicks > DateTime.MaxValue.Ticks)
        {
            return DateTimeOffset.MinValue;
        }

        return new DateTimeOffset(lGateTicks, TimeSpan.Zero);
    }
}
