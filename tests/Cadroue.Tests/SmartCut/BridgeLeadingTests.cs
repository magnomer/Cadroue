using Xunit;

namespace Cadroue.Tests;

public sealed class BridgeLeadingTests
{
    private const byte CraHeader = (21 << 1) | 0x01;
    private const byte BlaHeader = (18 << 1) | 0x01;
    private const byte IdrHeader = (20 << 1) | 0x01;
    private const byte VpsHeader = (32 << 1) | 0x01;

    [Fact]
    public void FirstCra_IsRewrittenToBla()
    {
        byte[] bytes = MdatCreate(VpsHeader, 0x00, CraHeader, 0x02);

        bool changed = TInterface.BridgeLeadingNormalize(bytes);

        Assert.True(changed);
        Assert.Equal(BlaHeader, bytes[^2]);
    }

    [Fact]
    public void FirstIdr_IsLeftUnchanged()
    {
        byte[] bytes = MdatCreate(IdrHeader, 0x02);
        byte[] original = (byte[])bytes.Clone();

        bool changed = TInterface.BridgeLeadingNormalize(bytes);

        Assert.False(changed);
        Assert.Equal(original, bytes);
    }

    [Fact]
    public void WithoutMdat_ReportsNoChange()
    {
        byte[] bytes = [0x00, 0x00, 0x00, 0x08, (byte)'f', (byte)'r', (byte)'e', (byte)'e'];

        Assert.False(TInterface.BridgeLeadingNormalize(bytes));
    }

    // Builds a minimal ISO-BMFF file: one mdat box holding length-prefixed NAL units,
    // each given here as a (header, second) pair.
    private static byte[] MdatCreate(params byte[] nalBytes)
    {
        var content = new List<byte>();
        for (int index = 0; index + 1 < nalBytes.Length; index += 2)
        {
            content.AddRange([0x00, 0x00, 0x00, 0x02, nalBytes[index], nalBytes[index + 1]]);
        }

        int size = 8 + content.Count;
        var buffer = new List<byte>
        {
            (byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size,
            (byte)'m', (byte)'d', (byte)'a', (byte)'t'
        };
        buffer.AddRange(content);
        return buffer.ToArray();
    }
}
