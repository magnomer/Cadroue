using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TBridgeFailure
{
    [Fact]
    public void MissingMiddleDecodeCutoff_DoesNotConvertSmartToWholeEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate("missing-source.mp4", 1, 5, "Include");
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeMissingBuild(work);

        LEncodeStage copy = Assert.Single(stages);
        Assert.Equal("Copying", copy.LEncodeStageLabel);
        Assert.Equal(
            "copy",
            TEncodeToken.TEncodeOptionRead(TEncodeToken.TEncodeTokenRead(copy.LEncodeStageArguments), "-c:v"));
    }

    [Fact]
    public void AudioProbeFailure_DoesNotConvertSmartToWholeEncode()
    {
        using var fixture = new TBridgeFixture();
        string source = Path.Combine(fixture.TBridgeFixtureRoot, "unprobeable-source.mkv");
        File.WriteAllText(source, "not a media file");
        using var environment = new TEncodeCommand();
        LWorkItem work = TBridgeFixture.TBridgeWorkCreate(source, 1.1, 6.4, "Include");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work,
            TEncodeCommand.TBridgeSpanCreate(1.1, 6.4),
            TEncodeCommand.TBridgeSpanCreate(1.1, 2),
            TEncodeCommand.TBridgeSpanCreate(2, 6, 5.933),
            TEncodeCommand.TBridgeSpanCreate(6, 6.4));

        TBridgeMiddleCheck(stages);
    }

    private static void TBridgeMiddleCheck(IReadOnlyList<LEncodeStage> stages)
    {
        Assert.Contains(stages, stage => stage.LEncodeStageLabel == "Copying middle");
        Assert.Contains(
            stages,
            stage => TEncodeToken.TEncodeOptionRead(
                TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments),
                "-c:v") == "copy");
        Assert.False(stages.Count == 1 && stages[0].LEncodeStageLabel == "Encoding");
    }
}
