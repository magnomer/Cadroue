using System;

using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class NeutralSampleTests
{
    private const int Width = 32;
    private const int Height = 24;

    [Fact]
    public void Resolve_NeutralGray_YieldsUnityGains()
    {
        byte[] pixels = FrameFill(128, 128, 128, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.Equal(1, sample.LNeutralRedGain, 3);
        Assert.Equal(1, sample.LNeutralGreenGain, 3);
        Assert.Equal(1, sample.LNeutralBlueGain, 3);
    }

    [Fact]
    public void Resolve_WarmCast_NeutralizesChannelsAndReducesRed()
    {
        byte[] pixels = FrameFill(180, 160, 130, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.True(sample.LNeutralRedGain < sample.LNeutralBlueGain);
        AssertNeutralized(sample);
    }

    [Fact]
    public void Resolve_CoolCast_NeutralizesChannelsAndReducesBlue()
    {
        byte[] pixels = FrameFill(130, 160, 180, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.True(sample.LNeutralBlueGain < sample.LNeutralRedGain);
        AssertNeutralized(sample);
    }

    [Fact]
    public void Resolve_NoisyOutliers_DoNotDominateMedian()
    {
        byte[] pixels = FrameFill(128, 128, 128, 255);
        // A handful of blown/black single pixels inside the region must not move the result.
        SetPixel(pixels, 16, 12, 255, 0, 255, 255);
        SetPixel(pixels, 14, 10, 0, 255, 0, 255);
        SetPixel(pixels, 18, 14, 255, 255, 0, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.Equal(128, sample.LNeutralRed);
        Assert.Equal(128, sample.LNeutralGreen);
        Assert.Equal(128, sample.LNeutralBlue);
        Assert.Equal(1, sample.LNeutralRedGain, 3);
        Assert.Equal(1, sample.LNeutralGreenGain, 3);
        Assert.Equal(1, sample.LNeutralBlueGain, 3);
    }

    [Fact]
    public void Resolve_ClippedEdgeRegion_StillResolves()
    {
        byte[] pixels = FrameFill(140, 150, 160, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 0, 0);

        Assert.True(sample.LNeutralResolved);
        AssertNeutralized(sample);
    }

    [Fact]
    public void Resolve_TransparentPixels_AreIgnored()
    {
        // Whole frame is transparent garbage; only the neutral opaque pixels are sampled.
        byte[] pixels = FrameFill(0, 255, 0, 0);
        for (int y = 7; y <= 17; y++)
        {
            for (int x = 11; x <= 21; x++)
            {
                SetPixel(pixels, x, y, 128, 128, 128, 255);
            }
        }

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.Equal(128, sample.LNeutralGreen);
        Assert.Equal(1, sample.LNeutralGreenGain, 3);
    }

    [Fact]
    public void Resolve_AllTransparent_FailsEmpty()
    {
        byte[] pixels = FrameFill(200, 200, 200, 0);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.Equal(LNeutralOutcome.LNeutralOutcomeEmpty, sample.LNeutralOutcome);
        AssertSafeFailure(sample);
    }

    [Fact]
    public void Resolve_BlackSample_FailsDark()
    {
        byte[] pixels = FrameFill(4, 6, 8, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.Equal(LNeutralOutcome.LNeutralOutcomeDark, sample.LNeutralOutcome);
        AssertSafeFailure(sample);
    }

    [Theory]
    [InlineData(-1, 12)]
    [InlineData(12, -1)]
    [InlineData(Width, 12)]
    [InlineData(16, Height)]
    public void Resolve_CoordinateOutsideFrame_FailsOutside(int x, int y)
    {
        byte[] pixels = FrameFill(128, 128, 128, 255);

        LNeutralSample sample = TNeutral.Resolve(pixels, Width, Height, x, y);

        Assert.Equal(LNeutralOutcome.LNeutralOutcomeOutside, sample.LNeutralOutcome);
        AssertSafeFailure(sample);
    }

    [Fact]
    public void Resolve_IsRepeatable()
    {
        byte[] pixels = FrameFill(175, 158, 132, 255);

        LNeutralSample first = TNeutral.Resolve(pixels, Width, Height, 16, 12);
        LNeutralSample second = TNeutral.Resolve(pixels, Width, Height, 16, 12);

        Assert.Equal(first, second);
    }

    [Fact]
    public void WhiteResolve_NeutralGray_YieldsUnityGains()
    {
        byte[] pixels = FrameFill(128, 128, 128, 255);

        LNeutralSample sample = TNeutral.WhiteResolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.Equal(1, sample.LNeutralRedGain, 3);
        Assert.Equal(1, sample.LNeutralGreenGain, 3);
        Assert.Equal(1, sample.LNeutralBlueGain, 3);
    }

    [Fact]
    public void WhiteResolve_WarmCast_LiftsToMaxChannelAndNeutralizes()
    {
        // Mild cast so no channel needs more than the 2x lift cap; full neutralization.
        byte[] pixels = FrameFill(170, 160, 150, 255);

        LNeutralSample sample = TNeutral.WhiteResolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        // White target = brightest channel (red here): red stays put, deficient
        // channels are only lifted, never pushed down.
        Assert.Equal(1, sample.LNeutralRedGain, 3);
        Assert.True(sample.LNeutralGreenGain >= 1);
        Assert.True(sample.LNeutralBlueGain >= 1);
        Assert.True(sample.LNeutralRedGain < sample.LNeutralBlueGain);
        AssertNeutralized(sample);
    }

    [Fact]
    public void WhiteResolve_BrightNeutral_StaysUnity()
    {
        // A near-white neutral pick: lenient picker must not blow it up, gains ~1.
        byte[] pixels = FrameFill(240, 240, 240, 255);

        LNeutralSample sample = TNeutral.WhiteResolve(pixels, Width, Height, 16, 12);

        Assert.True(sample.LNeutralResolved);
        Assert.Equal(1, sample.LNeutralRedGain, 3);
        Assert.Equal(1, sample.LNeutralGreenGain, 3);
        Assert.Equal(1, sample.LNeutralBlueGain, 3);
    }

    private static void AssertNeutralized(LNeutralSample sample)
    {
        AssertGainBounds(sample);
        double red = LinearRead(sample.LNeutralRed) * sample.LNeutralRedGain;
        double green = LinearRead(sample.LNeutralGreen) * sample.LNeutralGreenGain;
        double blue = LinearRead(sample.LNeutralBlue) * sample.LNeutralBlueGain;
        Assert.Equal(red, green, 3);
        Assert.Equal(green, blue, 3);
    }

    private static void AssertSafeFailure(LNeutralSample sample)
    {
        Assert.False(sample.LNeutralResolved);
        Assert.Equal(1, sample.LNeutralRedGain);
        Assert.Equal(1, sample.LNeutralGreenGain);
        Assert.Equal(1, sample.LNeutralBlueGain);
    }

    private static void AssertGainBounds(LNeutralSample sample)
    {
        foreach (double gain in new[] { sample.LNeutralRedGain, sample.LNeutralGreenGain, sample.LNeutralBlueGain })
        {
            Assert.True(double.IsFinite(gain));
            Assert.InRange(gain, 0, 2);
        }
    }

    private static double LinearRead(int channel)
    {
        double value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static byte[] FrameFill(byte r, byte g, byte b, byte a)
    {
        byte[] pixels = new byte[Width * Height * 4];
        for (int i = 0; i < Width * Height; i++)
        {
            pixels[(i * 4) + 0] = r;
            pixels[(i * 4) + 1] = g;
            pixels[(i * 4) + 2] = b;
            pixels[(i * 4) + 3] = a;
        }

        return pixels;
    }

    private static void SetPixel(byte[] pixels, int x, int y, byte r, byte g, byte b, byte a)
    {
        int index = ((y * Width) + x) * 4;
        pixels[index + 0] = r;
        pixels[index + 1] = g;
        pixels[index + 2] = b;
        pixels[index + 3] = a;
    }
}
