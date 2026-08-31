using System;

using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TNeutralWheel
{
    [Fact]
    public void ColorResolve_Centre_YieldsNeutralUnityGains()
    {
        LNeutralSample sample = TNeutral.TNeutralColorResolve(0, 0);

        Assert.True(sample.LNeutralResolved);
        Assert.Equal(sample.LNeutralRed, sample.LNeutralGreen);
        Assert.Equal(sample.LNeutralGreen, sample.LNeutralBlue);
        Assert.Equal(1, sample.LNeutralRedGain, 3);
        Assert.Equal(1, sample.LNeutralGreenGain, 3);
        Assert.Equal(1, sample.LNeutralBlueGain, 3);
    }

    [Theory]
    [InlineData(0.6, 0.0)]
    [InlineData(-0.5, 0.3)]
    [InlineData(0.2, -0.7)]
    [InlineData(-0.4, -0.4)]
    public void WheelResolve_InvertsColorResolve(double x, double y)
    {
        // A wheel pick reconstructs a gray; placing that gray back on the wheel must
        // land on the same disc coordinate (value is irrelevant to the cast direction).
        LNeutralSample sample = TNeutral.TNeutralColorResolve(x, y);
        LNeutralWheel wheel = TNeutral.TNeutralWheelResolve(
            sample.LNeutralRed, sample.LNeutralGreen, sample.LNeutralBlue);

        Assert.True(wheel.LNeutralWheelPresent);
        Assert.Equal(x, wheel.LNeutralWheelX, 2);
        Assert.Equal(y, wheel.LNeutralWheelY, 2);
    }

    [Fact]
    public void ColorResolve_ClampsToDiscEdge()
    {
        // Coordinates outside the unit disc still yield a valid, bounded sample.
        LNeutralSample sample = TNeutral.TNeutralColorResolve(3, 4);

        Assert.True(sample.LNeutralResolved);
        foreach (double gain in new[] { sample.LNeutralRedGain, sample.LNeutralGreenGain, sample.LNeutralBlueGain })
        {
            Assert.True(double.IsFinite(gain));
            Assert.InRange(gain, 0, 2);
        }
    }

    [Fact]
    public void WheelResolve_NeutralGray_IsCentred()
    {
        LNeutralWheel wheel = TNeutral.TNeutralWheelResolve(128, 128, 128);

        Assert.True(wheel.LNeutralWheelPresent);
        Assert.Equal(0, Math.Sqrt((wheel.LNeutralWheelX * wheel.LNeutralWheelX)
            + (wheel.LNeutralWheelY * wheel.LNeutralWheelY)), 3);
    }

    [Fact]
    public void WheelResolve_Black_IsUnset()
    {
        LNeutralWheel wheel = TNeutral.TNeutralWheelResolve(0, 0, 0);

        Assert.False(wheel.LNeutralWheelPresent);
    }

    [Fact]
    public void AnalyzeResolve_UniformFrame_MatchesDirectWheel()
    {
        byte[] pixels = TNeutralFrameCreate(4, 6, 180, 160, 130, 255);

        LNeutralWheel wheel = TNeutral.TNeutralAnalyzeResolve(
            pixels, 4, 6, LWhitebalanceMethod.LWhitebalanceMethodAverage);
        LNeutralWheel direct = TNeutral.TNeutralWheelResolve(180, 160, 130);

        Assert.True(wheel.LNeutralWheelPresent);
        Assert.Equal(direct.LNeutralWheelX, wheel.LNeutralWheelX, 3);
        Assert.Equal(direct.LNeutralWheelY, wheel.LNeutralWheelY, 3);
    }

    [Fact]
    public void AnalyzeResolve_TransparentPixels_AreIgnored()
    {
        byte[] pixels = new byte[4 * 4 * 4];
        for (int i = 0; i < 16; i++)
        {
            bool opaque = i < 8;
            pixels[(i * 4) + 0] = (byte)(opaque ? 200 : 0);
            pixels[(i * 4) + 1] = (byte)(opaque ? 150 : 255);
            pixels[(i * 4) + 2] = (byte)(opaque ? 120 : 0);
            pixels[(i * 4) + 3] = (byte)(opaque ? 255 : 0);
        }

        LNeutralWheel wheel = TNeutral.TNeutralAnalyzeResolve(
            pixels, 4, 4, LWhitebalanceMethod.LWhitebalanceMethodMedian);
        LNeutralWheel direct = TNeutral.TNeutralWheelResolve(200, 150, 120);

        Assert.Equal(direct.LNeutralWheelX, wheel.LNeutralWheelX, 3);
        Assert.Equal(direct.LNeutralWheelY, wheel.LNeutralWheelY, 3);
    }

    [Fact]
    public void AnalyzeResolve_EmptyPixels_IsUnset()
    {
        LNeutralWheel wheel = TNeutral.TNeutralAnalyzeResolve(
            Array.Empty<byte>(), 0, 0, LWhitebalanceMethod.LWhitebalanceMethodAverage);

        Assert.False(wheel.LNeutralWheelPresent);
    }

    private static byte[] TNeutralFrameCreate(int width, int height, byte r, byte g, byte b, byte a)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            pixels[(i * 4) + 0] = r;
            pixels[(i * 4) + 1] = g;
            pixels[(i * 4) + 2] = b;
            pixels[(i * 4) + 3] = a;
        }

        return pixels;
    }
}
