using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodeEdit
{
    private const string TEncodeVideoFilter = ",scale=in_range=full:out_range=tv,format=yuv420p";

    [Fact]
    public void ActiveVideoAdjustment_IsEmittedWhileInactiveAdjustmentIsOmitted()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkBrightnessCreate(true, 40),
            TInterface.TWorkContrastCreate(false, 150)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("eq=brightness=0.2", TEncodeToken.TEncodeOptionRead(tokens, "-vf"));
        Assert.DoesNotContain("contrast=", stage.LEncodeStageArguments, StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleActiveVideoAdjustments_FormOneFilterChain()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkBrightnessCreate(true, -80),
            TInterface.TWorkContrastCreate(true, 125)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, "-vf"));
        Assert.Equal("eq=brightness=-0.4:contrast=1.25", TEncodeToken.TEncodeOptionRead(tokens, "-vf"));
    }

    [Fact]
    public void ActiveSaturation_IsEmittedWhileInactiveSaturationIsOmitted()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo active = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkSaturationCreate(true, 150)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: active);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);
        Assert.Equal("eq=saturation=1.5", TEncodeToken.TEncodeOptionRead(tokens, "-vf"));

        LWorkVideo inactive = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkSaturationCreate(false, 150)
        });
        LWorkItem inactiveWork = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: inactive);

        Assert.DoesNotContain(
            "saturation=",
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(inactiveWork)).LEncodeStageArguments,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ActiveExposure_IsEmittedAsStandaloneFilterWhileInactiveIsOmitted()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo active = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkExposureCreate(true, 1.5)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: active);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);
        Assert.Equal("exposure=exposure=1.5" + TEncodeVideoFilter, TEncodeToken.TEncodeOptionRead(tokens, "-vf"));

        LWorkVideo inactive = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkExposureCreate(false, 1.5)
        });
        LWorkItem inactiveWork = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: inactive);

        Assert.DoesNotContain(
            "exposure=",
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(inactiveWork)).LEncodeStageArguments,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WhitebalanceExposureContrast_EmitInCanonicalOrder()
    {
        using var environment = new TEncodeCommand();
        LWorkVideo video = TInterface.TWorkVideoCreate(new[]
        {
            TInterface.TWorkContrastCreate(true, 150),
            TInterface.TWorkExposureCreate(true, 1.5),
            TInterface.TWorkWhitebalanceCreate(
                true, LWhitebalanceMethod.LWhitebalanceMethodAverage, 125)
        });
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindEdit, "source.mov", "edited.mp4", TEncodeCommand.TOutputCreate(),
            end: TimeSpan.FromMinutes(1), video: video);

        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(
            Assert.Single(TEncodeCommand.TEncodeStagesBuild(work)).LEncodeStageArguments);

        Assert.Equal(
            "colorcorrect=analyze=average:saturation=1.25,exposure=exposure=1.5,eq=contrast=1.5" + TEncodeVideoFilter,
            TEncodeToken.TEncodeOptionRead(tokens, "-vf"));
    }

}
