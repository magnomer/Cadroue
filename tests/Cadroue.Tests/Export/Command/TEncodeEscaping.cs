using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TEncodeEscaping
{
    [Fact]
    public void PathsContainingSpaces_RemainSingleInputAndOutputArguments()
    {
        using var environment = new TEncodeCommand();
        string source = Path.Combine("incoming files", "family video source.mov");
        string output = Path.Combine("finished files", "family video result.mp4");
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindConvert, source, output,
            TEncodeCommand.TOutputCreate(videoMode: "Copy", audioMode: "Copy"),
            end: TimeSpan.FromMinutes(1));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal(source, TEncodeToken.TEncodeOptionRead(tokens, "-i"));
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, source));
        Assert.Equal(output, tokens[^1]);
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(tokens, output));
    }

    [Fact]
    public void PresetValuesContainingSpaces_RemainSingleArguments()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TWorkCreate(
            LWorkKind.LWorkKindConvert, "source.mov", "result.mp4",
            TEncodeCommand.TOutputCreate(
                videoQuality: "23 -vf drawtext=text=x",
                videoSpeed: "slow -tune film",
                videoExtras: new Dictionary<string, string> { ["-tune"] = "film -an" },
                audioQuality: "192k -ac 1",
                audioChannels: "stereo -ar 8000"),
            end: TimeSpan.FromMinutes(1));

        LEncodeStage stage = Assert.Single(TEncodeCommand.TEncodeStagesBuild(work));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("23 -vf drawtext=text=x", TEncodeToken.TEncodeOptionRead(tokens, "-crf"));
        Assert.Equal("slow -tune film", TEncodeToken.TEncodeOptionRead(tokens, "-preset"));
        Assert.Equal("film -an", TEncodeToken.TEncodeOptionRead(tokens, "-tune"));
        Assert.Equal("192k -ac 1", TEncodeToken.TEncodeOptionRead(tokens, "-b:a"));
        Assert.Equal("stereo -ar 8000", TEncodeToken.TEncodeOptionRead(tokens, "-channel_layout"));
        Assert.DoesNotContain("-vf", tokens);
        Assert.DoesNotContain("-an", tokens);
        Assert.DoesNotContain("-ac", tokens);
        Assert.DoesNotContain("-ar", tokens);
    }
}
