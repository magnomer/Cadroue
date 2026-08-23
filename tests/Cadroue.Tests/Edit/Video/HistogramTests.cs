using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class HistogramTests
{
    [Fact]
    public void Histogram_CountsPerChannelAndLuminance()
    {
        // Two opaque pixels: pure red (255,0,0) and pure white (255,255,255).
        byte[] pixels =
        {
            255, 0, 0, 255,
            255, 255, 255, 255
        };

        LHistogramCounts counts = THistogram.Create(pixels, 2, 1);

        Assert.Equal(2, counts.LHistogramRed[255]);
        Assert.Equal(1, counts.LHistogramGreen[0]);
        Assert.Equal(1, counts.LHistogramGreen[255]);
        Assert.Equal(1, counts.LHistogramBlue[0]);
        Assert.Equal(1, counts.LHistogramBlue[255]);

        // Rec.709 luminance: red → 54, white → 255.
        Assert.Equal(1, counts.LHistogramLuminance[54]);
        Assert.Equal(1, counts.LHistogramLuminance[255]);
    }

    [Fact]
    public void Histogram_SkipsFullyTransparentPixels()
    {
        byte[] pixels =
        {
            10, 20, 30, 0,
            10, 20, 30, 255
        };

        LHistogramCounts counts = THistogram.Create(pixels, 2, 1);

        Assert.Equal(1, counts.LHistogramRed[10]);
        Assert.Equal(1, counts.LHistogramGreen[20]);
        Assert.Equal(1, counts.LHistogramBlue[30]);
    }

    [Fact]
    public void Histogram_EmptyDimensions_ReturnZeroedBins()
    {
        LHistogramCounts counts = THistogram.Create(System.Array.Empty<byte>(), 0, 0);

        Assert.All(counts.LHistogramLuminance, bin => Assert.Equal(0, bin));
        Assert.Equal(256, counts.LHistogramRed.Length);
    }
}
