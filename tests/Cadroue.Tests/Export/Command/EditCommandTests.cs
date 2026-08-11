using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class EditCommandTests
{
    [Fact]
    public void ActiveVideoAdjustment_IsEmittedWhileInactiveAdjustmentIsOmitted()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkBrightnessCreate(true, 40),
            TInterface.WorkContrastCreate(false, 150)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        LEncodeStage stage = Assert.Single(TEncodeCommand.StagesBuild(work));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("eq=brightness=0.2", CommandTokens.ValueAfter(tokens, "-vf"));
        Assert.DoesNotContain("contrast=", stage.LEncodeStageArguments, StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleActiveVideoAdjustments_FormOneFilterChain()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkBrightnessCreate(true, -80),
            TInterface.WorkContrastCreate(true, 125)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(1, CommandTokens.Count(tokens, "-vf"));
        Assert.Equal("eq=brightness=-0.4:contrast=1.25", CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Theory]
    [InlineData(-100, "eq=gamma=0.1")]
    [InlineData(-50, "eq=gamma=0.316")]
    [InlineData(0, "eq=gamma=1")]
    [InlineData(50, "eq=gamma=3.162")]
    [InlineData(100, "eq=gamma=10")]
    public void ActiveGamma_EmitsCompactInvariantEqFilter(double value, string expected)
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[] { TInterface.WorkGammaCreate(true, value) });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(expected, CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void InactiveGamma_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[] { TInterface.WorkGammaCreate(false, 50) });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.DoesNotContain("-vf", tokens);
    }

    [Fact]
    public void ActiveGamma_EmitsOrderedCompleteEqSegment()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 50, -50, 25, 75, 25)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(
            "eq=gamma=3.162:gamma_r=0.316:gamma_g=1.778:gamma_b=5.623:gamma_weight=0.75",
            CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void ActiveGamma_OmitsNeutralAdvancedTermsButForcesEncoding()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 0, 0, 0, 0, 0)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal("eq=gamma=1", CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void Gamma_QueueJson_RoundTripsCompletePayloadWithoutPrecisionLoss()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 20.125, -90.25, 10.375, 30.5, 25.625)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        LWorkItem restored = Assert.IsType<LWorkItem>(TInterface.WorkRecordRoundTrip(work));
        LWorkVideoStep gamma = Assert.Single(restored.LWorkVideo.LWorkVideoSteps);

        Assert.Equal(20.125, gamma.LWorkStepValue);
        Assert.NotNull(gamma.LWorkStepGamma);
        Assert.Equal(20.125, gamma.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(-90.25, gamma.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(10.375, gamma.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(30.5, gamma.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(25.625, gamma.LWorkStepGamma.LWorkGammaHighlightProtection);
    }

    [Fact]
    public void FlyleafGatedGamma_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.EditVideoCreate(
            new[] { TInterface.WorkGammaCreate(true, 50) }, gammaCapable: false);
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.DoesNotContain("-vf", tokens);
    }
}
