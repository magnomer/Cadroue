using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodeGamma
{
    private const string TEncodeVideoFilter = ",scale=in_range=full:out_range=tv,format=yuv420p";

    [Theory]
    [InlineData(-100, "eq=gamma=0.1")]
    [InlineData(-50, "eq=gamma=0.316")]
    [InlineData(0, "eq=gamma=1")]
    [InlineData(50, "eq=gamma=3.162")]
    [InlineData(100, "eq=gamma=10")]
    public void ActiveGamma_EmitsCompactInvariantEqFilter(double value, string expected)
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[] { TInterface.TWorkGammaCreate(true, value) });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(expected, TEncodeToken.TEncodeOptionRead(tokens, "-vf"));
    }

    [Fact]
    public void InactiveGamma_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[] { TInterface.TWorkGammaCreate(false, 50) });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.DoesNotContain("-vf", tokens);
    }

    [Fact]
    public void ActiveGamma_EmitsOrderedCompleteEqSegment()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkGammaCreate(true, 50, -50, 25, 75, 25)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(
            "eq=gamma=3.162:gamma_r=0.316:gamma_g=1.778:gamma_b=5.623:gamma_weight=0.75",
            TEncodeToken.TEncodeOptionRead(tokens, "-vf"));
    }

    [Fact]
    public void ActiveGamma_OmitsNeutralAdvancedTermsButForcesEncoding()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkGammaCreate(true, 0, 0, 0, 0, 0)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.NotEqual("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.Equal("eq=gamma=1", TEncodeToken.TEncodeOptionRead(tokens, "-vf"));
    }

    [Fact]
    public void Gamma_QueueJson_RoundTripsCompletePayloadWithoutPrecisionLoss()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkGammaCreate(true, 20.125, -90.25, 10.375, 30.5, 25.625)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        LWorkItem restored = Assert.IsType<LWorkItem>(TInterface.TWorkRecordMatch(work));
        LWorkVideoStep gamma = Assert.Single(restored.LWorkVideo.LWorkVideoSteps);

        Assert.Equal(20.125, gamma.LWorkStepValue);
        Assert.NotNull(gamma.LWorkStepGamma);
        Assert.Equal(20.125, gamma.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(-90.25, gamma.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(10.375, gamma.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(30.5, gamma.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(25.625, gamma.LWorkStepGamma.LWorkGammaHighlight);
    }

    [Fact]
    public void FlyleafGatedGamma_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TEditVideoCreate(
            new[] { TInterface.TWorkGammaCreate(true, 50) }, mpvOnlyCapable: false);
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.DoesNotContain("-vf", tokens);
    }

}
