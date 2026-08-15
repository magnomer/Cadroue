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
        Assert.Equal(25.625, gamma.LWorkStepGamma.LWorkGammaHighlight);
    }

    [Fact]
    public void FlyleafGatedGamma_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.EditVideoCreate(
            new[] { TInterface.WorkGammaCreate(true, 50) }, mpvOnlyCapable: false);
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.DoesNotContain("-vf", tokens);
    }

    [Theory]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodAverage, 100, "colorcorrect=analyze=average:saturation=1")]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMinmax, 0, "colorcorrect=analyze=minmax:saturation=0")]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMedian, 300, "colorcorrect=analyze=median:saturation=3")]
    [InlineData(LWhitebalanceMethod.LWhitebalanceMethodMedian, 123.75, "colorcorrect=analyze=median:saturation=1.238")]
    public void ActiveWhitebalance_EmitsExactCompactInvariantFilter(
        LWhitebalanceMethod method, double saturation, string expected)
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkWhitebalanceCreate(true, method, saturation)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(1, CommandTokens.Count(tokens, "-vf"));
        Assert.Equal(expected, CommandTokens.ValueAfter(tokens, "-vf"));
        Assert.DoesNotContain(',', CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void Whitebalance_UsesInvariantDecimalFormatting()
    {
        using var environment = new TEncodeCommand();
        System.Globalization.CultureInfo originalCulture =
            System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            LWorkVideo video = TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkWhitebalanceCreate(
                    true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 125)
            });
            LWorkItem work = TEncodeCommand.WorkCreate(
                LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
                end: TimeSpan.FromMinutes(1), video: video);

            IReadOnlyList<string> tokens = CommandTokens.Read(
                Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

            Assert.Equal(
                "colorcorrect=analyze=average:saturation=1.25",
                CommandTokens.ValueAfter(tokens, "-vf"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void InactiveWhitebalance_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkWhitebalanceCreate(
                false, LWhitebalanceMethod.LWhitebalanceMethodAverage, 150)
        });
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
    public void NeutralWhitebalance_ForcesEncoding()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMedian, 100)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal(
            "colorcorrect=analyze=median:saturation=1",
            CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Theory]
    [InlineData(1, 1, 1, 100, "colorchannelmixer=rr=1:gg=1:bb=1")]
    [InlineData(1.2, 1, 0.8, 100, "colorchannelmixer=rr=1.2:gg=1:bb=0.8")]
    [InlineData(5, 1, -1, 100, "colorchannelmixer=rr=2:gg=1:bb=0")]
    [InlineData(1.3755, 0.66667, 1, 100, "colorchannelmixer=rr=1.376:gg=0.667:bb=1")]
    [InlineData(1, 1, 1, 0, "colorchannelmixer=rr=1:gg=1:bb=1,eq=saturation=0")]
    [InlineData(1.2, 1, 0.8, 300, "colorchannelmixer=rr=1.2:gg=1:bb=0.8,eq=saturation=3")]
    public void ManualWhitebalance_EmitsDiagonalMixerAndSeparateSaturation(
        double red, double green, double blue, double saturation, string expected)
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkWhitebalanceManualCreate(true, saturation, red, green, blue, 0, 0, 0)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4",
            TEncodeCommand.OutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal(expected, CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void ManualWhitebalance_UsesInvariantDecimalFormatting()
    {
        using var environment = new TEncodeCommand();
        System.Globalization.CultureInfo originalCulture =
            System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            LWorkVideo video = TInterface.WorkVideoCreate(new[]
            {
                TInterface.WorkWhitebalanceManualCreate(true, 250, 1.2, 1, 0.8, 0, 0, 0)
            });
            LWorkItem work = TEncodeCommand.WorkCreate(
                LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
                end: TimeSpan.FromMinutes(1), video: video);

            IReadOnlyList<string> tokens = CommandTokens.Read(
                Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

            Assert.Equal(
                "colorchannelmixer=rr=1.2:gg=1:bb=0.8,eq=saturation=2.5",
                CommandTokens.ValueAfter(tokens, "-vf"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GammaAndManualWhitebalance_PreserveOrderInSeparateFilterSegments()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 50),
            TInterface.WorkWhitebalanceManualCreate(true, 200, 1.2, 1, 0.8, 0, 0, 0),
            TInterface.WorkContrastCreate(true, 150)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(1, CommandTokens.Count(tokens, "-vf"));
        Assert.Equal(
            "eq=gamma=3.162,colorchannelmixer=rr=1.2:gg=1:bb=0.8,eq=saturation=2,eq=contrast=1.5",
            CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void GammaAndWhitebalance_PreserveOrderInSeparateFilterSegments()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.WorkVideoCreate(new[]
        {
            TInterface.WorkGammaCreate(true, 50),
            TInterface.WorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 125),
            TInterface.WorkContrastCreate(true, 150)
        });
        LWorkItem work = TEncodeCommand.WorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.OutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = CommandTokens.Read(
            Assert.Single(TEncodeCommand.StagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(1, CommandTokens.Count(tokens, "-vf"));
        Assert.Equal(
            "eq=gamma=3.162,colorcorrect=analyze=average:saturation=1.25,eq=contrast=1.5",
            CommandTokens.ValueAfter(tokens, "-vf"));
    }

    [Fact]
    public void FlyleafGatedWhitebalance_OmitsFilterAndPreservesVideoCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.EditVideoCreate(new[]
        {
            TInterface.WorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodMinmax, 175)
        }, mpvOnlyCapable: false);
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
