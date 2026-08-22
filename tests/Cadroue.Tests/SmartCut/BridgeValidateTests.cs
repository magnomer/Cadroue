using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class BridgeValidateTests
{
    private static LBridgeStream Stream() => new(
        LBridgeCodec: "h264",
        LBridgeProfile: "High",
        LBridgeLevel: 40,
        LBridgeWidth: 1920,
        LBridgeHeight: 1080,
        LBridgePixel: "yuv420p",
        LBridgeFramerate: "30000/1001",
        LBridgeTimebase: "1/90000",
        LBridgeSampleAspect: "1:1",
        LBridgeDisplayAspect: "16:9",
        LBridgeFieldOrder: "progressive",
        LBridgeColorPrimaries: "bt709",
        LBridgeColorTransfer: "bt709",
        LBridgeColorMatrix: "bt709",
        LBridgeColorRange: "tv",
        LBridgeExtradata: new byte[] { 0x01, 0x64, 0x00, 0x28 },
        LBridgeContainer: "mp4");

    [Fact]
    public void IdenticalStreams_Compatible()
    {
        LBridgeCompatibility result = TInterface.BridgeValidate(Stream(), Stream());

        Assert.True(result.LBridgeCompatible);
        Assert.Equal(LBridgeReason.LBridgeReasonCompatible, result.LBridgeReason);
    }

    [Fact]
    public void DifferingExtradataContent_Incompatible()
    {
        LBridgeStream generated = Stream() with { LBridgeExtradata = new byte[] { 0x01, 0x64, 0x00, 0x29 } };

        LBridgeCompatibility result = TInterface.BridgeValidate(generated, Stream());

        Assert.False(result.LBridgeCompatible);
        Assert.Equal(LBridgeReason.LBridgeReasonExtradata, result.LBridgeReason);
    }

    [Theory]
    [MemberData(nameof(MismatchCases))]
    public void SinglePropertyMismatch_Incompatible(LBridgeStream generated, LBridgeReason expected)
    {
        LBridgeCompatibility result = TInterface.BridgeValidate(generated, Stream());

        Assert.False(result.LBridgeCompatible);
        Assert.Equal(expected, result.LBridgeReason);
    }

    public static IEnumerable<object[]> MismatchCases()
    {
        yield return new object[] { Stream() with { LBridgeCodec = "hevc" }, LBridgeReason.LBridgeReasonCodec };
        yield return new object[] { Stream() with { LBridgeProfile = "Main" }, LBridgeReason.LBridgeReasonProfile };
        yield return new object[] { Stream() with { LBridgeLevel = 41 }, LBridgeReason.LBridgeReasonLevel };
        yield return new object[] { Stream() with { LBridgeWidth = 1280 }, LBridgeReason.LBridgeReasonWidth };
        yield return new object[] { Stream() with { LBridgeHeight = 720 }, LBridgeReason.LBridgeReasonHeight };
        yield return new object[] { Stream() with { LBridgePixel = "yuv422p" }, LBridgeReason.LBridgeReasonPixel };
        yield return new object[] { Stream() with { LBridgeFramerate = "25/1" }, LBridgeReason.LBridgeReasonFramerate };
        yield return new object[] { Stream() with { LBridgeTimebase = "1/1000" }, LBridgeReason.LBridgeReasonTimebase };
        yield return new object[] { Stream() with { LBridgeSampleAspect = "4:3" }, LBridgeReason.LBridgeReasonSar };
        yield return new object[] { Stream() with { LBridgeDisplayAspect = "4:3" }, LBridgeReason.LBridgeReasonDar };
        yield return new object[] { Stream() with { LBridgeFieldOrder = "tt" }, LBridgeReason.LBridgeReasonField };
        yield return new object[] { Stream() with { LBridgeColorPrimaries = "bt2020" }, LBridgeReason.LBridgeReasonPrimaries };
        yield return new object[] { Stream() with { LBridgeColorTransfer = "smpte2084" }, LBridgeReason.LBridgeReasonTransfer };
        yield return new object[] { Stream() with { LBridgeColorMatrix = "bt2020nc" }, LBridgeReason.LBridgeReasonMatrix };
        yield return new object[] { Stream() with { LBridgeColorRange = "pc" }, LBridgeReason.LBridgeReasonRange };
        yield return new object[] { Stream() with { LBridgeContainer = "mkv" }, LBridgeReason.LBridgeReasonContainer };
    }

    [Fact]
    public void BitrateNotCarried_StructurallyEqualStreams_Compatible()
    {
        LBridgeCompatibility result = TInterface.BridgeValidate(Stream(), Stream());

        Assert.True(result.LBridgeCompatible);
        Assert.Equal(LBridgeReason.LBridgeReasonCompatible, result.LBridgeReason);
    }
}
