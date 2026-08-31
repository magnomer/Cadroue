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
}
