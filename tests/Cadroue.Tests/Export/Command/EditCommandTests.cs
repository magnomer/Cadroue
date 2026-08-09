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

        Assert.Equal("eq=brightness=0.1", CommandTokens.ValueAfter(tokens, "-vf"));
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
        Assert.Equal("eq=brightness=-0.2:contrast=1.25", CommandTokens.ValueAfter(tokens, "-vf"));
    }
}
